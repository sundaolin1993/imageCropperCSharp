using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace imageCropper
{
    public partial class Form1 : Form
    {
        private ImageCropper _mouseCtrl;
        public Form1()
        {
            InitializeComponent();
        }



        private void Form1_Load(object sender, EventArgs e)
        {
            _mouseCtrl = new ImageCropper(panel1, Point.Empty, panel1.ClientSize);
            _mouseCtrl.StartTranslate += new EventHandler(_mouseCtrl_StartTranslate);
            _mouseCtrl.StopTranslate += new EventHandler(_mouseCtrl_StopTranslate);
            _mouseCtrl.BackColor = Color.Black;
            _mouseCtrl.Refresh();
        }
        #region 平移鼠标改变鼠标样式 
        private void _mouseCtrl_StartTranslate(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }
        private void _mouseCtrl_StopTranslate(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }
        #endregion

        private void openFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Multiselect = false;
            file.Title = "选择图片";
            file.Filter = "(*.jpg,*.png,*.jpeg,*.bmp,*tif,*tiff)|*.jpg;*.png;*.jpeg;*.bmp;*.tif;*.tiff";
            if (file.ShowDialog() == DialogResult.OK)
            {
                using (Bitmap img = new Bitmap(file.FileName))
                {
                    _mouseCtrl.SetBackImage(img);
                }
                _mouseCtrl.Refresh();
            }
        }

        private void saveFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfile = new SaveFileDialog();
            sfile.Title = "保存截取";
            sfile.Filter = "(*.png)|*.png;";
            sfile.FileName = "image" + DateTime.Now.ToString("fff");
            sfile.AddExtension = true;
            if (sfile.ShowDialog() == DialogResult.OK)
            {
                if (_mouseCtrl.SaveFile(sfile.FileName))
                {
                    MessageBox.Show("ok");
                }
            }
        }
    }
}
