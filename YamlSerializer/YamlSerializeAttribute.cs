namespace System.Yaml.Serialization
{
    /// <summary>
    /// Specify the way to store a property or field of some class or structure.
    /// 
    /// See <see cref="YamlSerializer"/> for detail.
    /// </summary>
    /// <seealso cref="YamlSerializeAttribute"/>
    /// <seealso cref="YamlSerializer"/>
    public sealed class YamlSerializeAttribute: Attribute
    {
        internal YamlSerializeMethod SerializeMethod;
        /// <summary>
        /// Specify the way to store a property or field of some class or structure.
        /// 
        /// See <see cref="YamlSerializer"/> for detail.
        /// </summary>
        /// <seealso cref="YamlSerializeAttribute"/>
        /// <seealso cref="YamlSerializer"/>
        /// <param name="SerializeMethod">
        ///  <para>
        ///  - Never:   The property / field will not be stored.</para>
        ///  
        ///  <para>
        ///  - Assign:  When restored, new object is created by using the parameters in
        ///             the YAML data and assigned to the property / field. When the
        ///             property / filed is writeable, this is the default.</para>
        ///  
        ///  <para>
        ///  - Content: Only valid for a property / field that has a class or struct type.
        ///             When restored, instead of recreating the whole class or struct,
        ///             the members are independently restored. When the property / field
        ///             is not writeable this is the default.</para>
        /// 
        ///  <para>
        ///  - Binary:  Only valid for a property / field that has an  array type of a 
        ///             some value type. The content of the array is stored in a binary
        ///             format encoded in base64 style.</para>
        /// 
        /// </param>
        public YamlSerializeAttribute(YamlSerializeMethod SerializeMethod)
        {
            this.SerializeMethod = SerializeMethod;
        }
    }
}