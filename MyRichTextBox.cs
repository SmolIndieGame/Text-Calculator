using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Text_Caculator
{
    public class MyRichTextBox : RichTextBox
    {
        const short WM_PAINT = 0x00f;

        public static bool _Paint = true;
        protected override void WndProc(ref Message m)
        {
            // Code courtesy of Mark Mihevc  
            // sometimes we want to eat the paint message so we don't have to see all the  
            // flicker from when we select the text to change the color.  
            if (m.Msg == WM_PAINT)
            {
                if (_Paint)
                    base.WndProc(ref m); // if we decided to paint this control, just call the RichTextBox WndProc  
                else
                    m.Result = IntPtr.Zero; // not painting, must set this to IntPtr.Zero if not painting therwise serious problems.  
            }
            else
                base.WndProc(ref m); // message other than WM_PAINT, jsut do what you normally do.  
        }
    }
}
