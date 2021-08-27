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
            var description = new ApiParamDescription();

            return description;
        }

        public ApiMethodDescription GetApiMethodFullDescription(string methodName)
        {
            //var atr = GetApiMethodDescription(methodName);
            var paramReturn = type.GetMethod(methodName).ReturnParameter.GetCustomAttributes();
            var param = type.GetMethod(methodName).GetParameters().Select(x => Tuple.Create(x.Name, x.CustomAttributes.FirstOrDefault().ConstructorArguments));
            var description = new ApiMethodDescription { MethodDescription = new CommonDescription(methodName, GetApiMethodDescription(methodName)) };
            var paramList = new List<ApiParamDescription>();

            foreach (var item in param)
            {
                if (item.Item2.FirstOrDefault().Value != null)
                    paramList.Add(new ApiParamDescription() { ParamDescription = new CommonDescription(item.Item1), Required = (bool)item.Item2.FirstOrDefault().Value });
                else paramList.Add(new ApiParamDescription() { ParamDescription = new CommonDescription(item.Item1) });
            }

            description.ParamDescriptions = paramList.ToArray();


            foreach (var item in paramReturn)
            {
                var itemType = typeof(item);
                if (item.GetType() == typeof(bool)) description.ReturnDescription = new ApiParamDescription() { Required =  };
            }


            return description;

            //return new ApiMethodDescription
            //{
            //    MethodDescription = new CommonDescription(),
            //    ParamDescriptions = new[]
            //    {
            //        new ApiParamDescription()
            //    }
            //};
        }
    }
}