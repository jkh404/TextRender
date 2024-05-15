using System.Diagnostics;
using TextRender;
using TextRender.Abstracts;
using UtfUnknown;
using TextRender.SkiaSharpRender;
using TextRender.Command;
using System.Text;
using System.Runtime;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        private readonly BufferedGraphicsContext bufferedGraphicsContext;
        private readonly Graphics graphics;
        private BufferedGraphics bufferedGraphics;
        private System.Threading.Timer timer;
        Font textFont = new Font("宋体", 10);

        private int width = 1920;
        private int height = 1080;
        //private readonly string filePath = "《恶灵国度》作者：弹指一笑间0.txt";
        private readonly string filePath = "《恶灵国度》作者：弹指一笑间0_make.txt";

        //private readonly string text;
        private object renderLockObj = new object();
        private TextFrame textFrame;
        private Bitmap bitmap;

        private string[] FamilyNames = ["黑体", "华文新魏", "幼圆", "等线", "隶书", "楷体", "微软雅黑", "SimSum-ExtB"];
        private int curFamilyName;
        public Form1()
        {

            InitializeComponent();
            //using var read = File.OpenRead(filePath);
            //using var write = File.OpenWrite("《恶灵国度》作者：弹指一笑间0_make.txt");
            //for (int i = 0; i < 100; i++)
            //{
            //    read.CopyTo(write);
            //    read.Seek(0, SeekOrigin.Begin);

            //}
            //write.Flush();


            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(width, height);
            this.vScrollBar1.Location=new Point(this.ClientSize.Width-this.vScrollBar1.Width, 0);
            this.vScrollBar1.Height=this.ClientSize.Height;

            this.Text=$"{this.ClientSize.Width}×{this.ClientSize.Height}";
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.AllPaintingInWmPaint, true);
            this.Resize+=Form1_Resize;
            graphics=this.CreateGraphics();
            bufferedGraphicsContext = new BufferedGraphicsContext();
            bufferedGraphics =bufferedGraphicsContext.Allocate(graphics, this.ClientRectangle);

            this.FormClosing+=Form1_FormClosing;
            this.MouseWheel+=Form1_MouseWheel;
            this.MouseDown+=Form1_MouseDown; ;
            DetectionResult result = CharsetDetector.DetectFromFile(filePath);

            bitmap=new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //for (int i = 0; i < 10; i++)
            //{
            //    var path = filePath;
            //    var encoding = result.Detected.Encoding;
            //    using (var reader = new StreamReader(path, encoding))
            //    {
            //        var text=reader.ReadToEnd();
            //    }
            //}
            //var text = "";
            var path = filePath;
            var encoding = result.Detected.Encoding;




            using var reader = new StreamReader(path, encoding);
            var text=reader.ReadToEnd();





            //var byteArr = File.ReadAllBytes(path);
            //byteArr=null;


            //encoding.GetCharCount()
            //text=null;

            //GC.Collect(2, GCCollectionMode.Optimized);


            //for (int i = 0; i < 10000000; i++)
            //{
            //    var test = $"{i}{i}{i}{i}{i}{i}{i}{i}{i}{i}{i}{i}";
            //}


            var g = GraphicInstances.Instance.CreateSkiaSharpFrame();
            textFrame = new TextFrame(g, width, height);
            foreach (var item in FamilyNames)
            {
                g.FontProvider.LoadFont(new FontInfo
                {
                    Color=Colors.Black,
                    Size=20,
                    FamilyName=item,
                    FontStyle=TextRender.Command.FontStyle.Fill,
                    Spacing=0
                }, item);
            }
            textFrame.CurrentFontkey=FamilyNames[0];
            textFrame.PageMarginLeft=0;
            textFrame.PageMarginTop=10;
            textFrame.PageMarginBottom=10;
            textFrame.PageMarginRight=this.vScrollBar1.Width;
            //textFrame.InitText(text[0..4000]);
            textFrame.InitText(text);
            textFrame.Alloc();

            //for (int i = 0; i < 10; i++)
            //{
            //    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            //    Thread.Sleep(1000);
            //    GC.Collect();

            //}

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GCSettings.LatencyMode=GCLatencyMode.LowLatency;
            GC.Collect();
            Thread.Sleep(1000);
            GC.Collect();



            //textFrame.InitText(text[0..2000]);

            //textFrame.Invoke(t =>
            //{

            //});

            this.Load+=Form1_Load;
        }


        private void Form1_Resize(object? sender, EventArgs e)
        {
            lock (renderLockObj)
            {

                width=this.ClientSize.Width;
                height=this.ClientSize.Height;
                this.vScrollBar1.Location=new Point(this.ClientSize.Width-this.vScrollBar1.Width, 0);
                this.vScrollBar1.Height=this.ClientSize.Height;
                if (bufferedGraphics!=null) bufferedGraphics?.Dispose();

                if (bitmap!=null) bitmap?.Dispose();
                if (textFrame!=null)
                {

                    textFrame.Invoke(t =>
                    {
                        t.Resize(width, height);
                        //t.FixUpText();
                        //t.InitAlloc();
                    });
                }
                bitmap=new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                bufferedGraphics =bufferedGraphicsContext.Allocate(graphics, this.ClientRectangle);
            }
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            Thread thread = new Thread(Start);
            thread.IsBackground=true;
            thread.Start();
        }
        private void Start(object? obj)
        {
            Stopwatch stopwatch = new Stopwatch();
            double drawTime = 0;
            double tickTime = 0;
            double Fps = 0;
            timer = new System.Threading.Timer((_) =>
            {
                var Milliseconds = drawTime/10000.0;
                if (Milliseconds==0) Milliseconds=1;
                Fps =(Fps+(1000/Milliseconds))/2;
                drawTime+=1000*10000.0;
                if (Fps<10) Debug.WriteLine("渲染卡顿");
            }, null, 0, 1000);




            Random random = Random.Shared;
            //try
            //{
            //    while (Visible && bufferedGraphics!=null)
            //    {
            //        lock (renderLockObj)
            //        {
            //            var gh = bufferedGraphics.Graphics;
            //            stopwatch.Restart();
            //            gh.Clear(System.Drawing.Color.White);

            //            textFrame.Render();
            //            textFrame.CopyTo(bitmap);

            //            gh.DrawImage(bitmap, 0, 0);
            //            TextRenderer.DrawText(gh, $"FPS:{Fps:N2}", textFont, Point.Empty, System.Drawing.Color.Black);



            //            if (tickTime/10000.0>17)
            //            {
            //                tickTime=0;

            //                bufferedGraphics?.Render();

            //            }
            //            //bufferedGraphics?.Render();
            //            stopwatch.Stop();
            //            drawTime =stopwatch.ElapsedTicks;
            //            tickTime+=drawTime;
            //        }



            //    }
            //    Debug.WriteLine("结束");
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex);
            //}
            bool isBack = false;
            double jindu2 = 0;
            while (Visible && bufferedGraphics!=null)
            {
                lock (renderLockObj)
                {
                    var gh = bufferedGraphics.Graphics;
                    if (gh==null) continue;
                    stopwatch.Restart();
                    gh.Clear(System.Drawing.Color.White);
                    //textFrame.PageMarginTop-=0.1F;
                    Action<TextFrame> action = t =>
                    {
                        //if (!isBack) t.JumpToDisplay(jindu2+=0.01);
                        //else t.JumpToDisplay(jindu2-=0.01);

                        if(gunjindu>=0) t.JumpToDisplay(gunjindu);
                        this.gunjindu=-1;
                    };
                    this.textFrame?.Invoke(action);

                    textFrame?.Render();
                    textFrame?.CopyTo(bitmap);

                    gh.DrawImage(bitmap, 0, 0);
                    var jindu = textFrame?.DisplayRange.End/(textFrame?.TextLength*1D)*100;
                    TextRenderer.DrawText(gh, $"FPS:{Fps:N2}", textFont, Point.Empty, System.Drawing.Color.Black);
                    TextRenderer.DrawText(gh, $"滚动进度:{jindu:N2}%", textFont, new Point(100, 0), System.Drawing.Color.Black);
                    if (jindu>95)
                    {
                        isBack=true;
                    }
                    //TextRenderer.DrawText($"{}");

                    if (tickTime/10000.0>17)
                    {
                        tickTime=0;

                        //bufferedGraphics?.Render();

                    }

                    bufferedGraphics?.Render();
                    stopwatch.Stop();
                    drawTime =stopwatch.ElapsedTicks;
                    tickTime+=drawTime;
                }



            }
            Debug.WriteLine("结束");
        }
        //private void Start(object? obj)
        //{
        //    Stopwatch stopwatch = new Stopwatch();
        //    double drawTime = 0;
        //    double tickTime = 0;
        //    double Fps = 0;
        //    timer = new System.Threading.Timer((_) => {
        //        var Milliseconds = drawTime/10000.0;
        //        if (Milliseconds==0) Milliseconds=1;
        //        Fps =(Fps+(1000/Milliseconds))/2;
        //    }, null, 0, 1000);

        //    int i = 0;
        //    byte[] color = [0, 0, 0, 255];
        //    ref byte b = ref color[i%3];
        //    ref byte g = ref color[(i+1)%3];
        //    ref byte r = ref color[(i+2)%3];
        //    ref int argb = ref Unsafe.As<byte, int>(ref color[0]);
        //    while (Visible)
        //    {


        //        var gh = bufferedGraphics.Graphics;
        //        stopwatch.Restart();
        //        gh.Clear(Color.FromArgb(argb));
        //        TextRenderer.DrawText(gh, $"FPS:{Fps:N2}", textFont, Point.Empty, Color.Black);
        //        if (tickTime/10000.0>17)
        //        {
        //            tickTime=0;
        //            bufferedGraphics.Render();

        //            r++;
        //            if (r==255)
        //            {
        //                i++;
        //                r= ref color[i%3];
        //                g = ref color[(i+1)%3];
        //                b = ref color[(i+2)%3];
        //            }
        //            if (r==255 && g==255 && b==255)
        //            {
        //                i++;
        //                r=0;
        //                g=0;
        //                b=0;
        //            }
        //        }
        //        stopwatch.Stop();
        //        drawTime =stopwatch.ElapsedTicks;
        //        tickTime+=drawTime;


        //    }
        //}
        protected override void OnBackColorChanged(EventArgs e)
        {

        }
        protected override void OnBackgroundImageChanged(EventArgs e)
        {

        }
        protected override void OnBackgroundImageLayoutChanged(EventArgs e)
        {
        }
        protected override void OnParentBackColorChanged(EventArgs e)
        {

        }
        protected override void OnParentBackgroundImageChanged(EventArgs e)
        {

        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {

        }
        protected override void OnPaint(PaintEventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Visible=false;
            bufferedGraphics?.Dispose();
            bufferedGraphics=null;
            bufferedGraphicsContext?.Dispose();
            e.Cancel=false;
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {

            Action<TextFrame> action = t =>
            {
                //t.FontSize+=e.Delta/100.0F;

                //t.MoveDisplayStart(e.Delta<0 ? 1 : -1);
                //t.CurrentFontkey=FamilyNames[curFamilyName%FamilyNames.Length];
                //t.PageMarginTop+=e.Delta/100;

                t.MoveLineDisplay(e.Delta<0 ? 1 : -1);
            };
            this.textFrame?.Invoke(action);
            curFamilyName++;

        }

        private void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            //if (e.Button==MouseButtons.Right)
            //{
            //    int i = 40;
            //    while (--i>=0)
            //    {
            //        this.textFrame.Invoke(t =>
            //        {
            //            t.PageMarginTop+=-6;
            //        });
            //        Thread.Sleep(20);
            //    }
            //}
            //textFrame?.Dispose();
            //textFrame =null;
        }
        double gunjindu = 0;
        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            gunjindu=this.vScrollBar1.Value/(this.vScrollBar1.Maximum*1.0D);

        }
    }
}
