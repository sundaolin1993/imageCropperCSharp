using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace imageCropper
{
    public class ImageCropper : IDisposable
    {
        protected ImageCropper()
        { }

        public ImageCropper(Control parent, Point location, Size size)
        {
            _ctrl = new PictureBox();
            _zoomMatrix = new Matrix();
            m_Rect = new Rectangle(0, 0, 0, 0);
            _ctrl.Location = location;
            _ctrl.Size = size;
            _ctrl.Parent = parent;
            RegisterEvent();
        }


        #region 变量
        /// <summary>
        /// 创建选区还是移动图片
        /// </summary>
        private bool _moveOrSelect = false;
        /// <summary>
        /// 用于内部激活是否重绘
        /// </summary>
        private bool _mEnable = true;
        /// <summary>
        /// 显示的控件
        /// </summary>
        private PictureBox _ctrl;
        /// <summary>
        /// 背景纹理画刷(设置图像后会将图像转换成纹理画刷，因为填充画刷的速度要比绘制图像的速度快得多)
        /// </summary>
        private TextureBrush _tBrush;

        /// <summary>
        /// 控件的矩形区域
        /// </summary>
        private Rectangle m_Rect;

        /// <summary>
        /// 图像宽度
        /// </summary>
        private int _imgWidth = 1;

        /// <summary>
        /// 图像高度
        /// </summary>
        private int _imgHeight = 1;
        /// <summary>
        /// 缩放系数
        /// </summary>
        private float _zoom = 1.0f;

        /// <summary>
        /// 图片左上角X轴的偏移量
        /// </summary>
        private float _imgX = 0.0f;
        /// <summary>
        /// 图片左上角Y轴的偏移量
        /// </summary>
        private float _imgY = 0.0f;

        /// <summary>
        /// 按下平移键，记录当前的_imgX
        /// </summary>
        private float _imgStartX = 0.0f;

        /// <summary>
        /// 按下平移键，记录当前的_imgY
        /// </summary>
        private float _imgStartY = 0.0f;

        /// <summary>
        /// 当前的缩放矩阵
        /// </summary>
        private Matrix _zoomMatrix;

        /// <summary>
        /// 最小缩放倍率
        /// </summary>
        private float _minZoom = 0.08f;

        /// <summary>
        /// 最大缩放倍率
        /// </summary>
        private float _maxZoom = 50.0f;

        /// <summary>
        /// 每次鼠标滚轮时，缩放系数的增量值
        /// </summary>
        private float _mouseZoomDV = 0.03f;

        /// <summary>
        /// 平移按键
        /// </summary>
        private MouseButtons _translateBtn = MouseButtons.Right;

        /// <summary>
        /// 指示是否正在执行平移操作
        /// </summary>
        private bool _inTranslate = false;
        /// <summary>
        /// 按下鼠标记录鼠标的坐标位置
        /// </summary>
        Point basepoint = new Point();
        #endregion

        #region 属性

        /// <summary>
        /// 获取或设置背景颜色
        /// </summary>
        public Color BackColor
        {
            get { return _ctrl.BackColor; }
            set { _ctrl.BackColor = value; }
        }

        /// <summary>
        /// 获取是否正在执行平移操作
        /// </summary>
        public bool InTranslate
        {
            get { return _inTranslate; }
            protected set
            {
                if (value != _inTranslate)
                {
                    _inTranslate = value;
                    if (value)
                    {
                        OnStartTranslate();
                    }
                    else
                    {
                        OnStopTranslate();
                    }
                }
            }
        }


        #endregion

        #region 方法



        /// <summary>
        /// 注册事件
        /// </summary>
        public void RegisterEvent()
        {
            _ctrl.MouseDown += new MouseEventHandler(_ctrl_MouseDown);
            _ctrl.MouseMove += new MouseEventHandler(_ctrl_MouseMove);
            _ctrl.MouseUp += new MouseEventHandler(_ctrl_MouseUp);
            _ctrl.MouseEnter += new EventHandler(_ctrl_MouseEnter);
            _ctrl.MouseWheel += new MouseEventHandler(_ctrl_MouseWheel);
            _ctrl.Paint += new PaintEventHandler(_ctrl_Paint);
        }



        /// <summary>
        /// 释放背景纹理画刷
        /// </summary>
        protected virtual void tBrushDispose()
        {
            if (_tBrush != null)
            {
                _tBrush.Dispose();
                _tBrush = null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            tBrushDispose();
            if (_zoomMatrix != null)
            {
                _zoomMatrix.Dispose();
                _zoomMatrix = null;
            }
            if (_ctrl != null)
            {
                _ctrl.Dispose();
                _ctrl = null;
            }
        }

        /// <summary>
        /// 强制触发重绘事件
        /// </summary>
        public void Refresh()
        {
            _ctrl.Refresh();
        }

        /// <summary>
        /// 设置背景图像
        /// </summary>
        /// <param name="img"></param>
        public void SetBackImage(Bitmap img)
        {
            _mEnable = false;

            tBrushDispose();        //释放当前纹理
            if (img != null)
            {
                _imgWidth = img.Width;
                _imgHeight = img.Height;



                //当图像的水平分辨率和垂直分辨率不为96时，将图像转换成纹理画刷之后会导致图像自动被裁剪掉一部分
                if (img.VerticalResolution != 96 || img.HorizontalResolution != 96)
                {
                    //重新实例化一张图片之后，新图片的水平分辨率和垂直分辨率会自动调整为96
                    using (Bitmap bmp = new Bitmap(img))
                    {
                        _tBrush = new TextureBrush(img, WrapMode.Clamp);
                    }
                }
                else
                {
                    _tBrush = new TextureBrush(img, WrapMode.Clamp);
                }
            }
            else
            {
                _imgWidth = 1;
                _imgHeight = 1;

            }
            NormalMatrix();


            _mEnable = true;
        }

        /// <summary>
        /// 缩放图像
        /// </summary>
        /// <param name="x">缩放的中心点X</param>
        /// <param name="y">缩放的中心点Y</param>
        /// <param name="zoom">缩放系数</param>
        protected void ZoomImage(int x, int y, float zoom)
        {
            if (zoom > _maxZoom)
                zoom = _maxZoom;
            else if (zoom < _minZoom)
                zoom = _minZoom;
            float oldX = x / _zoom;
            float oldY = y / _zoom;
            _zoom = zoom;
            _imgX = x / zoom - oldX + _imgX;
            _imgY = y / zoom - oldY + _imgY;
            _zoomMatrix.Reset();
            _zoomMatrix.Scale(zoom, zoom);
            _zoomMatrix.Translate(_imgX, _imgY);
            _ctrl.Refresh();
        }


        /// <summary>
        /// 归一化当前矩阵
        /// </summary>
        protected virtual void NormalMatrix()
        {
            _zoom = 1.0f;
            _imgX = 0.0f;
            _imgY = 0.0f;
            _zoomMatrix.Reset();
            _zoomMatrix.Scale(_zoom, _zoom);
            _zoomMatrix.Translate(_imgX, _imgY);
        }
        /// <summary>
        /// 保存截取的图片
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool SaveFile(string fileName)
        {
            if (_tBrush == null)
            {
                return false;
            }
            if (_tBrush.Image != null)
            {
                Bitmap map = new Bitmap(_tBrush.Image);
                map = KiCut(map, m_Rect.X, m_Rect.Y, m_Rect.Width, m_Rect.Height);
                if (map == null)
                {
                    return false;
                }
                map.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
            }
            return true;
        }
        private static Bitmap KiCut(Bitmap b, int StartX, int StartY, int iWidth, int iHeight)
        {
            if (b == null)
            {
                return null;
            }

            int w = b.Width;
            int h = b.Height;
            if (StartX >= w || StartY >= h)
            {
                return null;
            }

            if (StartX + iWidth > w)
            {
                iWidth = w - StartX;
            }

            if (StartY + iHeight > h)
            {
                iHeight = h - StartY;
            }
            try
            {
                Bitmap bmpOut = new Bitmap(iWidth, iHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(bmpOut);
                g.DrawImage(b, new Rectangle(0, 0, iWidth, iHeight), new Rectangle(StartX, StartY, iWidth, iHeight), GraphicsUnit.Pixel);
                g.Dispose();
                return bmpOut;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region 控件事件

        private void _ctrl_Paint(object sender, PaintEventArgs e)
        {
            if (_mEnable)
            {
                Graphics g = e.Graphics;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;         //设置插值模式 
                if (_tBrush != null)
                {
                    g.Transform = _zoomMatrix;                                      //设置变换矩阵
                    g.FillRectangle(_tBrush, 0, 0, _imgWidth, _imgHeight);
                    if (_moveOrSelect)
                    {
                        Pen p = new Pen(Color.Red, 2.0f / _zoom);
                        p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawRectangle(p, this.m_Rect);
                    }
                }
            }
        }

        private void _ctrl_MouseWheel(object sender, MouseEventArgs e)
        {
            int a = e.Delta;
            if (_mEnable)
            {
                if (e.Delta >= 0)//正数为放大 负数为缩小
                {
                    float s = _zoom + _mouseZoomDV;
                    //ZoomImage(0, 0, _zoom + _mouseZoomDV);//0,0为图片左上角的坐标
                    ZoomImage(e.X, e.Y, _zoom + _mouseZoomDV);
                }
                else
                {
                    float s = _zoom - _mouseZoomDV;
                    //ZoomImage(0, 0, _zoom - _mouseZoomDV);//0,0为图片左上角的坐标
                    ZoomImage(e.X, e.Y, _zoom - _mouseZoomDV);
                }
            }
            _imgStartX = _imgX;
            _imgStartY = _imgY;
        }

        private void _ctrl_MouseEnter(object sender, EventArgs e)
        {
            //为控件设置焦点，因为只有控件有焦点才能为控件触发鼠标滚轮事件
            _ctrl.Focus();
        }

        private void _ctrl_MouseUp(object sender, MouseEventArgs e)
        {
            _imgStartX = _imgX;
            _imgStartY = _imgY;
            if (_mEnable)
            {
                InTranslate = false;
                _moveOrSelect = false;
            }
        }
        private void _ctrl_MouseDown(object sender, MouseEventArgs e)
        {
            if (_mEnable)
            {
                basepoint = e.Location;
                _imgStartX = _imgX;
                _imgStartY = _imgY;
                if (e.Button != _translateBtn)
                {
                    _moveOrSelect = true;
                }
                InTranslate = true;
            }
        }
        /// <summary>
        /// 移动鼠标发生的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">移动鼠标之后鼠标新的坐标</param>
        private void _ctrl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mEnable)
            {
                if (_inTranslate)
                {
                    if (_moveOrSelect)
                    {
                        //左键创建选区
                        float x = _imgStartX * -1;
                        float y = _imgStartY * -1;

                        float eex = (e.X / _zoom) + x;
                        float eey = (e.Y / _zoom) + y;
                        float bbx = (basepoint.X / _zoom) + x;
                        float bby = (basepoint.Y / _zoom) + y;

                        int ex = Convert.ToInt32(eex);
                        int ey = Convert.ToInt32(eey);
                        int bx = Convert.ToInt32(bbx);
                        int by = Convert.ToInt32(bby);
                        //ex ey 鼠标移动之后的坐标
                        //bx by 按下鼠标左键初始坐标

                        if (e.X < basepoint.X && e.Y < basepoint.Y)
                        {
                            m_Rect = new Rectangle(ex, ey, System.Math.Abs(ex - bx), System.Math.Abs(ey - by));
                        }
                        else if (e.X > basepoint.X && e.Y < basepoint.Y)
                        {
                            m_Rect = new Rectangle(bx, ey, System.Math.Abs(ex - bx), System.Math.Abs(ey - by));
                        }
                        else if (e.X < basepoint.X && e.Y > basepoint.Y)
                        {
                            m_Rect = new Rectangle(ex, by, System.Math.Abs(ex - bx), System.Math.Abs(ey - by));
                        }
                        else
                        {
                            m_Rect = new Rectangle(bx, by, System.Math.Abs(ex - bx), System.Math.Abs(ey - by));
                        }
                        _zoomMatrix.Reset();
                        _zoomMatrix.Scale(_zoom, _zoom);
                        _zoomMatrix.Translate(_imgStartX, _imgStartY);
                        _ctrl.Refresh();
                    }
                    else
                    {
                        //右键拖动图片

                        float x = e.X - basepoint.X;
                        float y = e.Y - basepoint.Y;
                        _imgX = x / _zoom + _imgStartX;
                        _imgY = y / _zoom + _imgStartY;
                        _zoomMatrix.Reset();
                        _zoomMatrix.Scale(_zoom, _zoom);
                        _zoomMatrix.Translate(_imgX, _imgY);
                        _ctrl.Refresh();
                    }
                }
            }
        }
        #endregion

        #region 自定义事件

        /// <summary>
        /// 在开始平移时发生
        /// </summary>
        public event EventHandler StartTranslate;

        /// <summary>
        /// 在结束平移时发生
        /// </summary>
        public event EventHandler StopTranslate;

        /// <summary>
        /// 触发开始平移事件
        /// </summary>
        protected virtual void OnStartTranslate()
        {
            if (StartTranslate != null)
            {
                StartTranslate(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 触发结束平移事件
        /// </summary>
        protected virtual void OnStopTranslate()
        {
            if (StopTranslate != null)
            {
                StopTranslate(this, EventArgs.Empty);
            }
        }

        #endregion


    }
}
