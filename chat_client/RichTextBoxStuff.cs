using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Documents;

namespace chat_client
{
   public static class RichTextBoxStuff
    {
       private enum EmfToWmfBitsFlags
       {

           // Use the default conversion
           EmfToWmfBitsFlagsDefault = 0x00000000,

           // Embedded the source of the EMF metafiel within the resulting WMF
           // metafile
           EmfToWmfBitsFlagsEmbedEmf = 0x00000001,

           // Place a 22-byte header in the resulting WMF file.  The header is
           // required for the metafile to be considered placeable.
           EmfToWmfBitsFlagsIncludePlaceable = 0x00000002,

           // Don't simulate clipping by using the XOR operator.
           EmfToWmfBitsFlagsNoXORClip = 0x00000004
       };

       private const int MM_ANISOTROPIC = 8;

       // Represents an unknown font family
       private const string FF_UNKNOWN = "UNKNOWN";

       // The number of hundredths of millimeters (0.01 mm) in an inch
       // For more information, see GetImagePrefix() method.
       private const int HMM_PER_INCH = 2540;

       // The number of twips in an inch
       // For more information, see GetImagePrefix() method.
       private const int TWIPS_PER_INCH = 1440;


       // The default text color
       private static RtfColor textColor;

       // The default text background color
       private static RtfColor highlightColor;

       // Dictionary that maps color enums to RTF color codes
       private static HybridDictionary rtfColor;

       // Dictionary that mapas Framework font families to RTF font families
       private static HybridDictionary rtfFontFamily;

       // The horizontal resolution at which the control is being displayed
       private static float xDpi;

       // The vertical resolution at which the control is being displayed
       private static float yDpi;
       
       private const string RTF_HEADER = @"{\rtf1\ansi\ansicpg1252\deff0\deflang1033";

       private static string RTF_IMAGE_POST = @"}";

        

       public static void EmbedImage(Image _image, RichTextBox rtb)
            {

                StringBuilder _rtf = new StringBuilder();

                // Append the RTF header
                _rtf.Append(RTF_HEADER);

                // Create the font table using the RichTextBox's current font and append
                // it to the RTF string
                _rtf.Append(GetFontTable(rtb.Font));

                using (Graphics _graphics = rtb.CreateGraphics())
                {
                    xDpi = _graphics.DpiX;
                    yDpi = _graphics.DpiY;
                }

                // Create the image control string and append it to the RTF string
                _rtf.Append(GetImagePrefix(_image));

                // Create the Windows Metafile and append its bytes in HEX format
                _rtf.Append(GetRtfImage(_image, rtb));

                // Close the RTF image control string
                _rtf.Append(RTF_IMAGE_POST);
                rtb.Invoke((Action) delegate
                {
                    rtb.SelectedRtf = _rtf.ToString();
                    //Debug debugForm = new Debug(_rtf.ToString());
                    //debugForm.Show();
                });
            }


       private static string GetImagePrefix(Image _image)
            {

                StringBuilder _rtf = new StringBuilder();

                // Calculate the current width of the image in (0.01)mm
                int picw = (int) Math.Round((_image.Width/xDpi)*HMM_PER_INCH);

                // Calculate the current height of the image in (0.01)mm
                int pich = (int) Math.Round((_image.Height/yDpi)*HMM_PER_INCH);

                // Calculate the target width of the image in twips
                int picwgoal = (int) Math.Round((_image.Width/xDpi)*TWIPS_PER_INCH);

                // Calculate the target height of the image in twips
                int pichgoal = (int) Math.Round((_image.Height/yDpi)*TWIPS_PER_INCH);

                // Append values to RTF string
                _rtf.Append(@"{\pict\wmetafile8");
                _rtf.Append(@"\picw");
                _rtf.Append(picw);
                _rtf.Append(@"\pich");
                _rtf.Append(pich);
                _rtf.Append(@"\picwgoal");
                _rtf.Append(picwgoal);
                _rtf.Append(@"\pichgoal");
                _rtf.Append(pichgoal);
                _rtf.Append(" ");

                return _rtf.ToString();
            }

            /// <summary>
            /// Use the EmfToWmfBits function in the GDI+ specification to convert a 
            /// Enhanced Metafile to a Windows Metafile
            /// </summary>
            /// <param name="_hEmf">
            /// A handle to the Enhanced Metafile to be converted
            /// </param>
            /// <param name="_bufferSize">
            /// The size of the buffer used to store the Windows Metafile bits returned
            /// </param>
            /// <param name="_buffer">
            /// An array of bytes used to hold the Windows Metafile bits returned
            /// </param>
            /// <param name="_mappingMode">
            /// The mapping mode of the image.  This control uses MM_ANISOTROPIC.
            /// </param>
            /// <param name="_flags">
            /// Flags used to specify the format of the Windows Metafile returned
            /// </param>
            [DllImportAttribute("gdiplus.dll")]
            private static extern uint GdipEmfToWmfBits(IntPtr _hEmf, uint _bufferSize,
                byte[] _buffer, int _mappingMode, EmfToWmfBitsFlags _flags);


            /// <summary>
            /// Wraps the image in an Enhanced Metafile by drawing the image onto the
            /// graphics context, then converts the Enhanced Metafile to a Windows
            /// Metafile, and finally appends the bits of the Windows Metafile in HEX
            /// to a string and returns the string.
            /// </summary>
            /// <param name="_image"></param>
            /// <returns>
            /// A string containing the bits of a Windows Metafile in HEX
            /// </returns>
            private static string GetRtfImage(Image _image, RichTextBox rtb)
            {

                StringBuilder _rtf = null;

                // Used to store the enhanced metafile
                MemoryStream _stream = null;

                // Used to create the metafile and draw the image
                Graphics _graphics = null;

                // The enhanced metafile
                Metafile _metaFile = null;

                // Handle to the device context used to create the metafile
                IntPtr _hdc;

                try
                {
                    _rtf = new StringBuilder();
                    _stream = new MemoryStream();

                    // Get a graphics context from the RichTextBox
                    using (_graphics = rtb.CreateGraphics())
                    {

                        // Get the device context from the graphics context
                        _hdc = _graphics.GetHdc();

                        // Create a new Enhanced Metafile from the device context
                        _metaFile = new Metafile(_stream, _hdc);

                        // Release the device context
                        _graphics.ReleaseHdc(_hdc);
                    }

                    // Get a graphics context from the Enhanced Metafile
                    using (_graphics = Graphics.FromImage(_metaFile))
                    {

                        // Draw the image on the Enhanced Metafile
                        _graphics.DrawImage(_image, new Rectangle(0, 0, _image.Width, _image.Height));

                    }

                    // Get the handle of the Enhanced Metafile
                    IntPtr _hEmf = _metaFile.GetHenhmetafile();

                    // A call to EmfToWmfBits with a null buffer return the size of the
                    // buffer need to store the WMF bits.  Use this to get the buffer
                    // size.
                    uint _bufferSize = GdipEmfToWmfBits(_hEmf, 0, null, MM_ANISOTROPIC,
                        EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);

                    // Create an array to hold the bits
                    byte[] _buffer = new byte[_bufferSize];

                    // A call to EmfToWmfBits with a valid buffer copies the bits into the
                    // buffer an returns the number of bits in the WMF.  
                    uint _convertedSize = GdipEmfToWmfBits(_hEmf, _bufferSize, _buffer, MM_ANISOTROPIC,
                        EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);

                    // Append the bits to the RTF string
                    for (int i = 0; i < _buffer.Length; ++i)
                    {
                        _rtf.Append(String.Format("{0:X2}", _buffer[i]));
                    }

                    return _rtf.ToString();
                }
                finally
                {
                    if (_graphics != null)
                        _graphics.Dispose();
                    if (_metaFile != null)
                        _metaFile.Dispose();
                    if (_stream != null)
                        _stream.Close();
                }
            }

    

            /// <summary>
            /// Creates a font table from a font object.  When an Insert or Append 
            /// operation is performed a font is either specified or the default font
            /// is used.  In any case, on any Insert or Append, only one font is used,
            /// thus the font table will always contain a single font.  The font table
            /// should have the form ...
            /// 
            /// {\fonttbl{\f0\[FAMILY]\fcharset0 [FONT_NAME];}
            /// </summary>
            /// <param name="_font"></param>
            /// <returns></returns>
            private static string GetFontTable(Font _font)
            {

                StringBuilder _fontTable = new StringBuilder();

                // Append table control string
                _fontTable.Append(@"{\fonttbl{\f0");
                _fontTable.Append(@"\");

                // If the font's family corresponds to an RTF family, append the
                // RTF family name, else, append the RTF for unknown font family.
                //if (rtfFontFamily.Contains(_font.FontFamily.Name))
                //    _fontTable.Append(rtfFontFamily[_font.FontFamily.Name]);
                //else
                //    _fontTable.Append(rtfFontFamily[FF_UNKNOWN]);

                // \fcharset specifies the character set of a font in the font table.
                // 0 is for ANSI.
                _fontTable.Append(@"\fcharset0 ");

                // Append the name of the font
                _fontTable.Append(_font.Name);

                // Close control string
                _fontTable.Append(@";}}");

                return _fontTable.ToString();
            }

            /// <summary>
            /// Creates a font table from the RtfColor structure.  When an Insert or Append
            /// operation is performed, _textColor and _backColor are either specified
            /// or the default is used.  In any case, on any Insert or Append, only three
            /// colors are used.  The default color of the RichTextBox (signified by a
            /// semicolon (;) without a definition), is always the first color (index 0) in
            /// the color table.  The second color is always the text color, and the third
            /// is always the highlight color (color behind the text).  The color table
            /// should have the form ...
            /// 
            /// {\colortbl ;[TEXT_COLOR];[HIGHLIGHT_COLOR];}
            /// 
            /// </summary>
            /// <param name="_textColor"></param>
            /// <param name="_backColor"></param>
            /// <returns></returns>
            private static string GetColorTable(RtfColor _textColor, RtfColor _backColor)
            {

                StringBuilder _colorTable = new StringBuilder();

                // Append color table control string and default font (;)
                _colorTable.Append(@"{\colortbl ;");

                // Append the text color
                _colorTable.Append(rtfColor[_textColor]);
                _colorTable.Append(@";");

                // Append the highlight color
                _colorTable.Append(rtfColor[_backColor]);
                _colorTable.Append(@";}\n");

                return _colorTable.ToString();
            }

            /// <summary>
            /// Called by overrided RichTextBox.Rtf accessor.
            /// Removes the null character from the RTF.  This is residue from developing
            /// the control for a specific instant messaging protocol and can be ommitted.
            /// </summary>
            /// <param name="_originalRtf"></param>
            /// <returns>RTF without null character</returns>
            private static string RemoveBadChars(string _originalRtf)
            {
                return _originalRtf.Replace("\0", "");
            }


    }
}
