using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
    public class VHHListView2 : ListView
    {
        // *******************************************************************************
        // 
        // Original Project "VHHListViewDemo"
        // Jan 2010 by Dave Chambers, dlchambers@aol.com
        // see: https://www.codeproject.com/KB/list/VHHListView.aspx
        //
        // *******************************************************************************
        //
        // - fixed a bug where the header was always placed at postion (x = 0, y = 0)
        //   without considering the track-position by a horizontal scrollbar
        // - the HDM_LAYOUT message is now processed using pointers directly
        //   instead of using Marshal.PtrToStructure and Marshal.StructureToPtr

        // member variables
        //==================================================================================

        int headerHeight        = 20;
        HeaderControl header    = null;

        // constructors
        //==================================================================================

        public VHHListView2()
        {
            base.View = View.Details;
            base.AutoArrange = false;
            base.TileSize = Size.Empty;
        }

        // prperties
        //==================================================================================

        [Category("Appearance"), DefaultValue(20)]
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always)]
        public int HeaderHeight
        {
            get => headerHeight; set
            {
                headerHeight = value;

                if (headerHeight < 4)
                    headerHeight = 4;

                if (headerHeight > 100)
                    headerHeight = 100;

                header?.UpdateHeight(true);
            }
        }

        // protected override
        //==================================================================================

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // create our custom header control, which subclasses the ListView's hdr control.
            header = new HeaderControl(this);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Release subclassed control
            header.ReleaseHandle();
            header = null;

            base.OnHandleDestroyed(e);
        }

        // nested types
        //==================================================================================

        class HeaderControl : NativeWindow
        {
            const int LVM_FIRST = 0x1000;
            const int LVM_GETHEADER = LVM_FIRST + 31;
            const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
            const int LVS_EX_FULLROWSELECT = 0x00000020;
            const int SWP_FRAMECHANGED = 32;
            const int HDM_FIRST = 0x1200;
            const int HDM_LAYOUT = (HDM_FIRST + 5);

            [StructLayout(LayoutKind.Sequential)]
            struct HDLAYOUT
            {
                public IntPtr prc;
                public IntPtr pwpos;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct WINDOWPOS
            {
                public IntPtr hwnd;
                public IntPtr hwndInsertAfter;
                public int x;
                public int y;
                public int cx;
                public int cy;
                public int flags;
            }

            [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            [DllImport("User32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

            readonly VHHListView2 parent = null;

            //----------------------------------------------------------------------------------

            public HeaderControl(VHHListView2 parent)
            {
                // Remember the ListView that owns this header
                this.parent = parent;

                // Ask the ListView for the handle (HWND) for it's (stock) header control
                IntPtr hHdr = SendMessage(parent.Handle, LVM_GETHEADER, 0, 0);

                if (hHdr == IntPtr.Zero)
                    throw new NullReferenceException("Tried to create HeaderControl before base.HeaderControl was created!");

                // Attach this control to the stock header
                AssignHandle(hHdr);

                // Set initial height upon creation
                UpdateHeight(false);
            }

            //----------------------------------------------------------------------------------

            public void UpdateHeight(bool invalidate)
            {
                // the ListView is already set to fullrowselect but sending this 
                // message is s simple way of triggering the HDM_LAYOUT message
                SendMessage(parent.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,
                    LVS_EX_FULLROWSELECT, LVS_EX_FULLROWSELECT);

                if (invalidate)
                {
                    parent.BeginInvoke((MethodInvoker)delegate {
                        InvalidateRect(Handle, IntPtr.Zero, true);
                        InvalidateRect(parent.Handle, IntPtr.Zero, true);
                    });
                }
            }

            //----------------------------------------------------------------------------------

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m); // retrieve required HDLAYOUT information first

                if (m.Msg == Win32.HDM_LAYOUT && parent.HeaderStyle > 0)
                {
                    unsafe
                    {
                        HDLAYOUT* hd = (Win32.HDLAYOUT*)m.LParam;
                        RECT* prc = (Win32.RECT*)hd->prc;
                        WINDOWPOS* pwpos = (Win32.WINDOWPOS*)hd->pwpos;

                        pwpos->cy = parent.ColumnHeight;    // *** APPLY NEW HEADER HEIGHT ***
                        prc->top = parent.ColumnHeight;     // *** TOP COORDINATE FOR THE FIRST LISTVIEW ITEM ***
                    }
                }
            }
        }

        // hidden member
        //==================================================================================

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new View View
        {
            get => View.Details;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new Size TileSize
        {
            get => Size.Empty;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool AutoArrange
        {
            get => false;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ListViewAlignment Alignment
        {
            get => ListViewAlignment.Left;
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new ImageList LargeImageList
        {
            get => null;
        }
    }
}
