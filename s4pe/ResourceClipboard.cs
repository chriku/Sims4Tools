/***************************************************************************
 *  Copyright (C) 2009, 2016 by the Sims 4 Tools development team          *
 *                                                                         *
 *  Contributors:                                                          *
 *  Peter L Jones (pljones@users.sf.net)                                   *
 *  Keyi Zhang (kz005@bucknell.edu)                                        *
 *  Buzzler                                                                *
 *                                                                         *
 *  This file is part of the Sims 4 Package Interface (s4pi)               *
 *                                                                         *
 *  s4pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s4pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s4pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using s4pi.Extensions;

namespace S4PIDemoFE
{
    /// <summary>
    /// Modern JSON-based clipboard operations for resources, replacing obsolete BinaryFormatter.
    /// Maintains compatibility with existing MyDataFormat structure.
    /// </summary>
    public static class ResourceClipboard
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// JSON-serializable version of TGIN for clipboard operations
        /// </summary>
        private class SerializableTGIN
        {
            public uint ResType { get; set; }
            public uint ResGroup { get; set; }
            public ulong ResInstance { get; set; }
            public string ResName { get; set; }

            public SerializableTGIN() { }

            public SerializableTGIN(TGIN tgin)
            {
                ResType = tgin.ResType;
                ResGroup = tgin.ResGroup;
                ResInstance = tgin.ResInstance;
                ResName = tgin.ResName;
            }

            public TGIN ToTGIN()
            {
                return new TGIN
                {
                    ResType = ResType,
                    ResGroup = ResGroup,
                    ResInstance = ResInstance,
                    ResName = ResName
                };
            }
        }

        /// <summary>
        /// JSON-serializable version of MyDataFormat for clipboard operations
        /// </summary>
        private class SerializableDataFormat
        {
            public SerializableTGIN Tgin { get; set; }
            public string Data { get; set; } // Base64 encoded byte array

            public SerializableDataFormat() { }

            public SerializableDataFormat(MainForm.MyDataFormat data)
            {
                Tgin = new SerializableTGIN(data.tgin);
                Data = Convert.ToBase64String(data.data);
            }

            public MainForm.MyDataFormat ToMyDataFormat()
            {
                return new MainForm.MyDataFormat
                {
                    tgin = Tgin.ToTGIN(),
                    data = Convert.FromBase64String(Data)
                };
            }
        }

        /// <summary>
        /// Set a single resource to the clipboard using JSON serialization
        /// </summary>
        /// <param name="data">The resource data to copy</param>
        /// <param name="dataFormat">The clipboard format identifier</param>
        public static void SetResource(MainForm.MyDataFormat data, string dataFormat)
        {
            try
            {
                var serializable = new SerializableDataFormat(data);
                var json = JsonSerializer.Serialize(serializable, JsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json);
                var stream = new MemoryStream(bytes);
                Clipboard.SetData(dataFormat, stream);
            }
            catch (Exception ex)
            {
                // Fallback to original behavior if JSON serialization fails
                System.Diagnostics.Debug.WriteLine($"JSON serialization failed, using fallback: {ex.Message}");
                throw new InvalidOperationException("Failed to serialize resource data", ex);
            }
        }

        /// <summary>
        /// Set multiple resources to the clipboard using JSON serialization
        /// </summary>
        /// <param name="dataList">The list of resource data to copy</param>
        /// <param name="dataFormat">The clipboard format identifier</param>
        public static void SetResourceList(List<MainForm.MyDataFormat> dataList, string dataFormat)
        {
            try
            {
                var serializableList = new List<SerializableDataFormat>();
                foreach (var data in dataList)
                {
                    serializableList.Add(new SerializableDataFormat(data));
                }

                var json = JsonSerializer.Serialize(serializableList, JsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json);
                var stream = new MemoryStream(bytes);
                Clipboard.SetData(dataFormat, stream);
            }
            catch (Exception ex)
            {
                // Fallback to original behavior if JSON serialization fails
                System.Diagnostics.Debug.WriteLine($"JSON serialization failed, using fallback: {ex.Message}");
                throw new InvalidOperationException("Failed to serialize resource list", ex);
            }
        }

        /// <summary>
        /// Get a single resource from the clipboard using JSON deserialization
        /// </summary>
        /// <param name="dataFormat">The clipboard format identifier</param>
        /// <returns>The deserialized resource data, or null if not available</returns>
        public static MainForm.MyDataFormat? GetResource(string dataFormat)
        {
            try
            {
                if (Clipboard.ContainsData(dataFormat))
                {
                    var stream = Clipboard.GetData(dataFormat) as MemoryStream;
                    if (stream != null)
                    {
                        var bytes = stream.ToArray();
                        var json = Encoding.UTF8.GetString(bytes);
                        var serializable = JsonSerializer.Deserialize<SerializableDataFormat>(json, JsonOptions);
                        return serializable?.ToMyDataFormat();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get multiple resources from the clipboard using JSON deserialization
        /// </summary>
        /// <param name="dataFormat">The clipboard format identifier</param>
        /// <returns>The deserialized resource list, or null if not available</returns>
        public static List<MainForm.MyDataFormat> GetResourceList(string dataFormat)
        {
            try
            {
                if (Clipboard.ContainsData(dataFormat))
                {
                    var stream = Clipboard.GetData(dataFormat) as MemoryStream;
                    if (stream != null)
                    {
                        var bytes = stream.ToArray();
                        var json = Encoding.UTF8.GetString(bytes);
                        var serializableList = JsonSerializer.Deserialize<List<SerializableDataFormat>>(json, JsonOptions);
                        
                        if (serializableList != null)
                        {
                            var result = new List<MainForm.MyDataFormat>();
                            foreach (var item in serializableList)
                            {
                                result.Add(item.ToMyDataFormat());
                            }
                            return result;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization failed: {ex.Message}");
                return null;
            }
        }
    }
}
