// Portions of this code
// Copyright (c) 2010, LizardTech, a Celartem company
// All rights reserved.
//                                                              
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice,
//       this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of LizardTech nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
/***************************************************************************
 *  Copyright (C) 2010 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Windows;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace S3PIDemoFE.DDSWidget
{
    class DDSRenderEngine : IRenderEngine
    {
        private Stream dds = null;
        private DDSSurface surface;
        private BitmapSource? currentTexture;

        public DDSRenderEngine(DDSSurface surface) 
        { 
            this.surface = surface; 
        }

        public void OnDeviceCreated(object sender, EventArgs e)
        {
            // No DirectX device needed for simplified approach
        }

        private void ClearTexture()
        {
            currentTexture = null;
        }

        public void OnDeviceDestroyed(object sender, EventArgs e) 
        { 
            ClearTexture(); 
        }

        public void OnDeviceLost(object sender, EventArgs e) 
        { 
            ClearTexture(); 
        }

        public void OnDeviceReset(object sender, EventArgs e)
        {
            if (dds == null) return;
            ClearTexture();
            LoadDDSTexture();
        }

        private void LoadDDSTexture()
        {
            if (dds == null) return;

            try
            {
                dds.Position = 0;
                
                // Convert DDS stream to BitmapSource for WPF display
                // For now, we'll try to load it as a standard image
                // TODO: Implement proper DDS decoding if needed
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = dds;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                currentTexture = bitmap;
                
                // Update the surface display
                surface.UpdateTexture(currentTexture);
            }
            catch (Exception ex)
            {
                // If direct loading fails, we might need DDS-specific decoding
                System.Diagnostics.Debug.WriteLine($"DDS loading error: {ex.Message}");
                // Fallback: create a placeholder image
                CreatePlaceholderTexture();
            }
        }

        private void CreatePlaceholderTexture()
        {
            // Create a simple placeholder when DDS can't be loaded directly
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                context.DrawRectangle(System.Windows.Media.Brushes.LightGray, 
                                    new System.Windows.Media.Pen(System.Windows.Media.Brushes.Gray, 1), 
                                    new Rect(0, 0, 256, 256));
                context.DrawText(
                    new FormattedText("DDS Preview\nNot Available", 
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    FlowDirection.LeftToRight,
                                    new Typeface("Arial"),
                                    14,
                                    System.Windows.Media.Brushes.Black,
                                    VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip),
                    new System.Windows.Point(20, 100));
            }

            var bitmap = new RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingVisual);
            bitmap.Freeze();
            
            currentTexture = bitmap;
            surface.UpdateTexture(currentTexture);
        }

        public void OnMainLoop(object sender, EventArgs e)
        {
            // No continuous rendering needed for static image display
        }

        public Stream DDS
        {
            set
            {
                if (dds != null) 
                { 
                    dds = null; 
                    ClearTexture(); 
                }
                dds = value;
                if (dds != null)
                {
                    LoadDDSTexture();
                }
                else
                {
                    surface.UpdateTexture(null);
                }
            }
        }
    }
}
