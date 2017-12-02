using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Schema
{
    /// <summary>
    /// Defines an interface for a content handler that supports one or more dynamic (not persisted) fields.
    /// </summary>
    public interface ISupportsDynamicFields
    {
        /// <summary>
        /// Returns dictionary that contains all dynamic field's descriptor. The keys are their names.
        /// </summary>
        IDictionary<string, FieldMetadata> GetDynamicFieldMetadata();
        /// <summary>
        /// Returns a property value by name. Well-known and dynamic properties can also be accessed here.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties return their value - otherwise call the base implementation.
        /// </summary>
        object GetProperty(string name);
        /// <summary>
        /// Assigns the given value to the named property. Well-known and dynamic properties can also be set.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties set their value - otherwise call the base implementation.
        /// </summary>
        void SetProperty(string name, object value);
        /// <summary>
        /// Gets true if the current instance is not saved yet. 
        /// </summary>
        bool IsNewContent { get; }
        /// <summary>
        /// Clears any computed cached data.
        /// </summary>
        void ResetDynamicFields();
    }
}
