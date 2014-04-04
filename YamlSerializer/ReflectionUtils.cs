using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace System.Yaml.Serialization
{
    static class ReflectionUtils
    {

        /// <summary>
        /// generates a method to do 'object.property = (propertytype)value;'
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Action<object, object> CreateSetPropertyMethod(PropertyInfo propertyInfo)
        {
            MethodInfo mi = propertyInfo.GetSetMethod();

            if (!propertyInfo.CanWrite || mi == null)
                return null;

            ParameterExpression targParam = Expression.Parameter(typeof(object), "obj");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "val");

            //convert parameters into their proper types
            UnaryExpression target = Expression.Convert(targParam, propertyInfo.DeclaringType);
            UnaryExpression value = Expression.Convert(valueParam, propertyInfo.PropertyType);

            //and call the setter on it.
            MethodCallExpression mce = Expression.Call(target, mi, value);

            return Expression.Lambda<Action<object, object>>(mce, targParam, valueParam).Compile();
        }

        /// <summary>
        /// generates a method to do 'return (object)(object.property);'
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Func<object, object> CreateGetPropertyMethod(PropertyInfo propertyInfo)
        {
            ParameterExpression targParam = Expression.Parameter(typeof(object), "obj");

            //convert the parameter into the correct target type
            UnaryExpression target = Expression.Convert(targParam, propertyInfo.DeclaringType);
            //get the property
            MemberExpression fieldExp = Expression.Property(target, propertyInfo);
            //convert to object to return it
            UnaryExpression retConvExp = Expression.Convert(fieldExp, typeof(object));
            
            var finalLambda = Expression.Lambda<Func<object, object>>(retConvExp, targParam);

            return finalLambda.Compile();
        }

        /// <summary>
        /// Generate a method to do 'return (object)object.field;'
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        public static Func<object, object> CreateGetFieldMethod(FieldInfo fieldInfo)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(
                string.Concat("_GetF", fieldInfo.Name, "_"),
                typeof(object),
                new Type[] { typeof(object) },
                fieldInfo.DeclaringType);
            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0); // push object onto the stack
            generator.Emit(OpCodes.Ldfld, fieldInfo); // .field

            if (fieldInfo.FieldType.IsValueType)
            {
                generator.Emit(OpCodes.Box, fieldInfo.FieldType); //boxing is required for value types...
            }
            generator.Emit(OpCodes.Ret); // return

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        /// <summary>
        /// Generate a method to do 'object.field = (fieldtype)value;'
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        public static Action<object, object> CreateSetFieldMethod(FieldInfo fieldInfo)
        {
            if (fieldInfo.DeclaringType.IsValueType)
            {
                if (fieldInfo.DeclaringType.StructLayoutAttribute != null)
                {
#if DEBUG
                    throw new FieldAccessException(string.Format("The type '{0}' has a StructLayoutAttribute, and thus cannot have field setter for '{1}' generated. otherwise the .net framework will throw an access exception. As a fix, either create a typeconverter for the type, turn the fields into properties, or remove the structlayout attribute.", fieldInfo.DeclaringType.FullName, fieldInfo.Name));
#else
                    Console.WriteLine("Using reflection to set field {1}.{0}, due to StructLayoutAttribute being defined. Recommended to define a type converter for the type instead.", fieldInfo.Name, fieldInfo.DeclaringType.Name);
#endif
                    
                    //fffffff. this has to be done via reflection, otherwise we're gonna get field access exceptions that will crash the program.
                    return new Action<object, object>((targ, value) => fieldInfo.SetValue(targ, value));
                }
            }

            DynamicMethod dynamicMethod = new DynamicMethod(
                    String.Concat("_SetF", fieldInfo.Name, "_"),
                    typeof(void),
                    new[] { typeof(object), typeof(object) },
                    fieldInfo.DeclaringType);
            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0); // push object onto the stack
            generator.Emit(OpCodes.Ldarg_1); // push value onto the stack
            if (fieldInfo.FieldType.IsValueType)
            {
                //unbox value to field's type
                generator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            }
            generator.Emit(OpCodes.Stfld, fieldInfo); // set field value
            generator.Emit(OpCodes.Ret); //return

            var setter = (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
            return setter;
        }
    }
}
