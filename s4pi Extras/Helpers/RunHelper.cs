using System;
using System.Collections.Generic;
using System.IO;
using s4pi.Interfaces;
using System.Windows.Forms;

namespace s4pi.Helpers
{
    public interface IRunHelper
    {
        byte[] Result { get; }
    }
    public static class RunHelper
    {
        public static int Run(Type mainForm, params string[] args)
        {
            bool useClipboard = false;
            bool useFile = false;
            List<string> files = new List<string>();

            if (!typeof(Form).IsAssignableFrom(mainForm) || !typeof(IRunHelper).IsAssignableFrom(mainForm))
            {
                CopyableMessageBox.Show("Invalid call to RunHelper.Run",
                    "Fail", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
                return -1;
            }

            List<char> switchChars = new List<char>(new char[] { '/', '-', });
            switchChars.Remove(Path.DirectorySeparatorChar);
            foreach (string s in args)
            {
                string p = s;
                if (p.Length > 1 && switchChars.Contains(p[0]))
                {
                    if ("clipboard".StartsWith(p.Substring(1).ToLower()))
                        useClipboard = true;
                    else
                    {
                        CopyableMessageBox.Show(String.Format("Unrecognised switch: \"{0}\"", p),
                            "Fail", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
                        return 1;
                    }
                }
                else
                    files.Add(s);
            }

            if (useClipboard && files.Count > 0)
            {
                CopyableMessageBox.Show("Do not use /Clipboard with other arguments",
                    "Fail", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
                return 2;
            }
            if (files.Count > 1)
            {
                CopyableMessageBox.Show("Only pass a single file argument",
                    "Fail", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
                return 3;
            }

            useFile = files.Count > 0;
            useClipboard = !useFile;

            Stream ms;

            if (useClipboard)
            {
                ms = GetStreamFromClipboard();
                if (ms == null)
                {
                    CopyableMessageBox.Show("Invalid clipboard content",
                        "Fail", CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
                    return 4;
                }
                Clipboard.Clear();
            }
            else
            {
                try
                {
                    ms = File.Open(files[0], FileMode.Open, FileAccess.ReadWrite);
                }
                catch (Exception ex)
                {
                    CopyableMessageBox.IssueException(ex, files[0] + "\n" + mainForm.Assembly.FullName, "Failed to open file");
                    return -1;
                }
            }


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            byte[] result = null;
            try
            {
                Form theForm = (Form)mainForm.GetConstructor(new Type[] { typeof(Stream), }).Invoke(new object[] { ms, });
                Environment.ExitCode = 1;
                Application.Run(theForm);
                if (Environment.ExitCode != 0)
                    return 0;

                result = ((IRunHelper)theForm).Result;
                if (result == null)
                    return 0;
            }
            catch (Exception ex)
            {
                CopyableMessageBox.IssueException(ex, mainForm.Assembly.FullName, "Program exception");
                return -1;
            }

            if (useClipboard)
            {
                SetStreamToClipboard(new MemoryStream(result));
            }
            else
            {
                ms.Position = 0;
                ms.SetLength(0);
                ms.Write(result, 0, result.Length);
            }

            return 0;
        }

        /// <summary>
        /// Modern JSON-based stream clipboard operations to replace obsolete DataFormats.Serializable
        /// </summary>
        private static void SetStreamToClipboard(Stream stream)
        {
            try
            {
                // Convert stream to base64 and store as text - more compatible than DataFormats.Serializable
                var bytes = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(bytes, 0, bytes.Length);
                var base64 = Convert.ToBase64String(bytes);
                var data = new { Type = "s4pe.stream", Data = base64, Size = bytes.Length };
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                Clipboard.SetText(json);
            }
            catch
            {
                // Fallback to original method if JSON fails
                Clipboard.SetData(DataFormats.Serializable, stream);
            }
        }

        /// <summary>
        /// Get stream from clipboard, supporting both new JSON format and legacy Serializable format
        /// </summary>
        private static MemoryStream GetStreamFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    // Try to parse as JSON first
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(text);
                    if (data.TryGetProperty("Type", out var typeElement) && 
                        typeElement.GetString() == "s4pe.stream" &&
                        data.TryGetProperty("Data", out var dataElement))
                    {
                        var base64 = dataElement.GetString();
                        if (base64 != null)
                        {
                            var bytes = Convert.FromBase64String(base64);
                            return new MemoryStream(bytes);
                        }
                    }
                }
                
                // Fallback to legacy format
                if (Clipboard.ContainsData(DataFormats.Serializable))
                {
                    return Clipboard.GetData(DataFormats.Serializable) as MemoryStream;
                }
            }
            catch
            {
                // If JSON parsing fails, try legacy format
                if (Clipboard.ContainsData(DataFormats.Serializable))
                {
                    return Clipboard.GetData(DataFormats.Serializable) as MemoryStream;
                }
            }
            return null;
        }
    }
}
