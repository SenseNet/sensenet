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
        /// Returns a dictionary that contains the dynamic fields' descriptors. The keys are their names.
        /// </summary>
        IDictionary<string, FieldMetadata> GetDynamicFieldMetadata();
        /// <summary>
        /// Returns a well-known or dynamic property value by name.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties return their value - otherwise call the base implementation.
        /// </summary>
        object GetProperty(string name);
        /// <summary>
        /// Assigns the given value to a well-known or dynamic property.
        /// In derived content handlers this should be overridden and in case of local strongly typed
        /// properties set their value - otherwise call the base implementation.
        /// </summary>
        void SetProperty(string name, object value);
        /// <summary>
        /// Gets true if the current instance is not saved yet. 
        /// </summary>
        bool IsNewContent { get; }
        /// <summary>
        /// Clears any computed cached data related to dynamic fields.
        /// </summary>
        void ResetDynamicFields();
    }
}
