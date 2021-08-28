using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Documentation
{
    public class Specifier<T> : ISpecifier
    {
        Type type = typeof(T);

        public string GetApiDescription()
        {
            return (string)type.CustomAttributes
                .Select(x => x.ConstructorArguments.FirstOrDefault().Value)
                .FirstOrDefault();
        }

        public string[] GetApiMethodNames()
        {
            return type.GetMethods()
                .Where(x => x.GetCustomAttributes().Any(y => y.GetType() == typeof(ApiMethodAttribute)))
                .Select(name => name.Name)
                .ToArray();
        }

        public string GetApiMethodDescription(string methodName)
        {
            var description = type.GetMethod(methodName);
            if (description == null || description.CustomAttributes.Count() == 0) return null;
            return description.GetCustomAttributes(true)
                .OfType<ApiDescriptionAttribute>()
                .FirstOrDefault()
                .Description;
        }

        public string[] GetApiMethodParamNames(string methodName)
        {
            return type.GetMethod(methodName).GetParameters().Select(x => x.Name).ToArray();
        }

        public string GetApiMethodParamDescription(string methodName, string paramName)
        {
            var method = type.GetMethod(methodName);

            if (method == null || method.GetParameters()
                .Where(x => x.Name == paramName)
                .FirstOrDefault() == null) return null;

            var result = method.GetParameters()
                .Where(x => x.Name == paramName)
                .Select(x => x.GetCustomAttribute<ApiDescriptionAttribute>())
                .FirstOrDefault();

            return result == null ? null : result.Description;
        }

        public ApiParamDescription GetApiMethodParamFullDescription(string methodName, string paramName)
        {
            if (type.GetMethod(methodName) == null)
                return new ApiParamDescription() { ParamDescription = new CommonDescription(paramName) };
            var paramDesc = type.GetMethod(methodName).GetParameters().Where(x => x.Name == paramName);
            var description = new ApiParamDescription() {ParamDescription = new CommonDescription(paramName)};

            foreach (var item in paramDesc.SelectMany(x => x.CustomAttributes))
            {
                if (item.AttributeType == typeof(ApiRequiredAttribute))
                    description.Required = (bool)item.ConstructorArguments.FirstOrDefault().Value;
                if (item.AttributeType == typeof(ApiDescriptionAttribute))
                    description.ParamDescription.Description = (string)item.ConstructorArguments.FirstOrDefault().Value;
                if (item.AttributeType == typeof(ApiIntValidationAttribute))
                {
                    description.MinValue = item.ConstructorArguments[0].Value;
                    description.MaxValue = item.ConstructorArguments[1].Value;
                }
            }

            return description;
        }

        public ApiMethodDescription GetApiMethodFullDescription(string methodName)
        {
            if (type.GetMethod(methodName).GetCustomAttributes<ApiMethodAttribute>(true).FirstOrDefault() == null)
                return null;

            var returnDesc = type.GetMethod(methodName).ReturnParameter.CustomAttributes;
            var paramDesc = type.GetMethod(methodName).GetParameters()
                .Select(x => Tuple.Create(x.Name, x.CustomAttributes.ToList())).ToList();

            var description = new ApiMethodDescription
            {
                MethodDescription = new CommonDescription(methodName, GetApiMethodDescription(methodName))
            };

            BuildApiDescParams(description, methodName);

            if (returnDesc.Count() != 0) BuildApiReturnParams(description, returnDesc);

            return description;
        }

        private void BuildApiDescParams(ApiMethodDescription description, string methodName)
        {
            var paramList = new List<ApiParamDescription>();

            foreach (var item in type.GetMethod(methodName).GetParameters()
                .Select(x => Tuple.Create(x.Name, x.CustomAttributes.ToList())).ToList())
            {
                paramList.Add(new ApiParamDescription() { ParamDescription = new CommonDescription(item.Item1) });

                foreach (var customParams in item.Item2)
                {
                    if (customParams.AttributeType == typeof(ApiRequiredAttribute)
                        && (bool)customParams.ConstructorArguments.FirstOrDefault().Value)
                        paramList.Last().Required = (bool)customParams.ConstructorArguments.FirstOrDefault().Value;
                    if (customParams.AttributeType == typeof(ApiDescriptionAttribute))
                        paramList.Last().ParamDescription.Description =
                            (string)customParams.ConstructorArguments.FirstOrDefault().Value;
                    if (customParams.AttributeType == typeof(ApiIntValidationAttribute))
                    {
                        paramList.Last().MinValue = customParams.ConstructorArguments[0].Value;
                        paramList.Last().MaxValue = customParams.ConstructorArguments[1].Value;
                    }
                }
            }
            description.ParamDescriptions = paramList.ToArray();
        }

        private void BuildApiReturnParams(ApiMethodDescription description, IEnumerable<CustomAttributeData> returnDesc)
        {
            description.ReturnDescription = new ApiParamDescription() { ParamDescription = new CommonDescription() };

            foreach (var item in returnDesc)
            {
                if (item.AttributeType == typeof(ApiRequiredAttribute))
                    description.ReturnDescription.Required = (bool)item.ConstructorArguments.FirstOrDefault().Value;
                if (item.AttributeType == typeof(ApiDescriptionAttribute))
                    description.ReturnDescription.ParamDescription.Description = 
                        (string)item.ConstructorArguments.FirstOrDefault().Value;
                if (item.AttributeType == typeof(ApiIntValidationAttribute))
                {
                    description.ReturnDescription.MinValue = item.ConstructorArguments[0].Value;
                    description.ReturnDescription.MaxValue = item.ConstructorArguments[1].Value;
                }
            }
        }
    }
}