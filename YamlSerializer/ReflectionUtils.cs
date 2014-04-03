using System;
using System.Collections.Generic;
using System.Linq;
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
            //setter is private or something
            if (!propertyInfo.CanWrite)
                return null;

            //or no setter defined at all...
            MethodInfo setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;

            DynamicMethod setter = new DynamicMethod(
              String.Concat("_Set", propertyInfo.Name, "_"),
              typeof(void),
              new[] { typeof(object), typeof(object) },
              propertyInfo.DeclaringType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);

            if (propertyInfo.PropertyType.IsValueType)
            {
                //need to unbox value into correct type for valuetypes
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
            }

            generator.Emit(OpCodes.Call, setMethod);
            generator.Emit(OpCodes.Ret);

            return (Action<object, object>)setter.CreateDelegate(typeof(Action<object, object>));
        }

        /// <summary>
        /// generates a method to do 'return (object)object.property;'
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Func<object, object> CreateGetPropertyMethod(PropertyInfo propertyInfo)
        {
            //no getter defined
            MethodInfo getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            DynamicMethod getter = new DynamicMethod(
              String.Concat("_Get", propertyInfo.Name, "_"),
              typeof(object),
              new[] { typeof(object) },
              propertyInfo.DeclaringType);
            ILGenerator generator = getter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0); // push object onto the stack
            generator.Emit(OpCodes.Call, getMethod); // .property

            if (propertyInfo.PropertyType.IsValueType)
            {
                //boxing is required for value types...
                generator.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }
            generator.Emit(OpCodes.Ret);//return

            return (Func<object, object>)getter.CreateDelegate(typeof(Func<object, object>));
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
