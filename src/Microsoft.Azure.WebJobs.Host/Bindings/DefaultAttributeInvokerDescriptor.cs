﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Host.Bindings
{
    // Helpers for providing default behavior for an IAttributeInvokeDescriptor that 
    // convert between a TAttribute and a string representation (invoke string). 
    // Properties with [AutoResolve] are the interesting ones to serialize and deserialize. 
    // Assume any property without a [AutoResolve] attribute is read-only and so doesn't need to be included in the invoke string. 
    internal static class DefaultAttributeInvokerDescriptor<TAttribute>
        where TAttribute : Attribute
    {
        public static TAttribute FromInvokeString(AttributeCloner<TAttribute> cloner, string invokeString)
        {
            if (invokeString == null)
            {
                throw new ArgumentNullException("invokeString");
            }

            // Instantiating new attributes can be tricky since sometimes the arg is to the ctor and sometimes
            // its a property setter. AttributeCloner already solves this, so use it here to do the actual attribute instantiation. 
            // This has an instantiation problem similar to what Attribute Cloner has 
            if (invokeString[0] == '{')
            {
                var propertyValues = JsonConvert.DeserializeObject<IDictionary<string, string>>(invokeString);

                var attr = cloner.New(propertyValues);
                return attr;
            }
            else
            {
                var attr = cloner.New(invokeString);
                return attr;
            }
        }

        public static string ToInvokeString(TAttribute source)
        {
            Dictionary<string, string> vals = new Dictionary<string, string>();
            foreach (var prop in typeof(TAttribute).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                bool resolve = prop.GetCustomAttribute<AutoResolveAttribute>() != null;
                if (resolve)
                {
                    var str = (string)prop.GetValue(source);
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        vals[prop.Name] = str;
                    }
                }
            }

            if (vals.Count == 0)
            {
                return string.Empty;
            }
            if (vals.Count == 1)
            {
                // Flat
                return vals.First().Value;
            }
            return JsonConvert.SerializeObject(vals);
        }
    }
}