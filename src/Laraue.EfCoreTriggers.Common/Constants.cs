namespace Laraue.EfCoreTriggers.Common
{
    /// <summary>
    /// Library constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// All triggers names starts with this key. Acronym from laraue core trigger.
        /// Note: if triggers already created with one prefix, it's changing will be
        /// a problem, because this prefix is using to find trigger annotations
        /// while migrations generating.
        /// The best way will be to generate a new migrations, manually fix they names
        /// to start from the new <see cref="AnnotationKey"/>, only then change the value.
        /// </summary>
        public static string AnnotationKey { get; set; } = "LC_TRIGGER";

        public const string TriggerAnnotationKey = "LC_TRIG";
        public const string NativeStoredProcedureAnnotationKey = "LC_SPROC_{0}_";
        public const string NativeUserDefinedTypeAnnotationKey = "LC_TYPE_{0}_";
        public const string NativeUserDefinedFunctionAnnotationKey = "LC_FUNC_{0}_";
        public const string NativeViewAnnotationKey = "LC_VIEW_{0}_";
        public const string NativeTriggerAnnotationKey = "LC_NTRIG_{0}_";
        public const string NativeIndexAnnotationKey = "LC_INDEX_{0}_";
    }
}