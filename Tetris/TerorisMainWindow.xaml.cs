using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TerorisMainWindow : Window
    {
        //
        // --- Fields ----
        //

        /// <summary>ゲームのフレームレートを指定します。</summary>
        private static readonly TimeSpan frame_rate = new TimeSpan(0, 0, 0, 0, 40);

        /// <summary>キャンバスに描画するタイマー値</summary>
        DispatcherTimer timer = new DispatcherTimer() { Interval = TerorisMainWindow.frame_rate };

        /// <summary>テトリスの画面</summary>
        private Bord bord;

        /// <summary>フレームをカウントします</summary>
        private int skip_frames;

        /// <summary>Key_Downイベント(左右下)のイベント発生時のガクつきを抑えるためのクラス</summary>
        private PeriodicSignals keySignal = new PeriodicSignals(TerorisMainWindow.frame_rate);

        /// <summary>Key_Downイベント(上)のイベント発生時のガクつきを抑えるためのクラス</summary>
        private PeriodicSignals keySignalUp = new PeriodicSignals(new TimeSpan(0, 0, 0, 0, TerorisMainWindow.frame_rate.Milliseconds * 5));

        //
        // ---- Constructors ----
        //

        /// <summary>
        /// 既定の初期値で <see cref="TerorisMainWindow"/> の新しいインスタンスを作成します。
        /// </summary>
        public TerorisMainWindow()
        {
            this.InitializeComponent();
            this.bord = new Bord();
            this.bord.InitBord();
            this.timer.Tick += timer_Tick;
            this.keySignal.Ticks += this.continuousKeyboardEvents;
            this.keySignalUp.Ticks += this.continuousKeyboardEvents;
        }

        //
        // ---- EventHandlers ----
        //

        /// <summary>
        /// MainWindow Class - Loadedイベント : ウインドウプロシージャの登録
        /// </summary>
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            timer.IsEnabled = true;

            this.bord.CurrentBlobkStatus = this.bord.CreateNewBlock();

            this.bord.PutBlock(this.bord.CurrentBlobkStatus);
        }

        /// <summary>
        /// MainWindow - KeyDwonイベント : ボタンが押された事をオブジェクトに通知します。
        /// </summary>
        private void canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                this.keySignalUp.Start(e.Key);
            }
            else
            {
                this.keySignal.Start(e.Key);
            }
        }

        /// <summary>
        /// MainWindow - KeyUpイベント : ボタンが押されたことをオブジェクトに通知します。
        /// </summary>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                this.keySignalUp.Stop();
            }
            else
            {
                this.keySignal.Stop();
            }
        }

        /// <summary>
        /// DispatcherTimer - Tickイベント : 1フレームの処理を記述します。
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            this.timer.IsEnabled = false;

            this.bord.ShowBord(this.game_view);

            if (this.skip_frames % 5 == 0)
            {
                if (!this.bord.DownCurrentBlock())
                {
                    this.bord.GameOver();
                    this.bord.ShowBord(this.game_view);
                    return;
                }
                this.skip_frames = 0;
            }

            this.skip_frames++;

            this.timer.IsEnabled = true;
        }

        //
        // ---- Private Methods ----
        //

        /// <summary>
        /// キーボードが押された時の処理
        /// </summary>
        private void continuousKeyboardEvents(Key key)
        {
            BlockStatus s = this.bord.CurrentBlobkStatus.Clone();
            switch (key)
            {
                case Key.Left:
                    s.X--;
                    break;

                case Key.Right:
                    s.X++;
                    break;

                case Key.Up:
                    s.Rotate++;
                    break;

                case Key.Down:
                    s.Y++;
                    break;

                default:
                    break;
            }

            if (s.X != this.bord.CurrentBlobkStatus.X ||
                s.Y != this.bord.CurrentBlobkStatus.Y ||
                s.Rotate != this.bord.CurrentBlobkStatus.Rotate)
            {
                this.bord.DeleteBlock(this.bord.CurrentBlobkStatus);
                if (this.bord.PutBlock(s))
                {
                    this.bord.CurrentBlobkStatus = s;
                }
                else
                {
                    this.bord.PutBlock(this.bord.CurrentBlobkStatus);
                    this.keySignal.Stop();
                    this.keySignalUp.Stop();
                }
            }
        }
    }

    /// <summary>
    /// ブロックを管理するためのクラス
    /// </summary>
    public class BlockImgHelper
    {
        /// <summary>ブロックの画像が配置されている場所</summary>
        private Uri block_img_uri = new Uri("./Resources/block2.bmp", UriKind.Relative);

        /// <summary>クラスで管理するイメージテーブル</summary>
        private IDictionary<BlockColors, CroppedBitmap> block_img_table = new Dictionary<BlockColors, CroppedBitmap>();

        // 各ブロックの画像
        private CroppedBitmap _block_1 = null; // 必要ないが警告でるので抑制がめんどくさいので設定しておく
        private CroppedBitmap _block_2 = null;
        private CroppedBitmap _block_3 = null;
        private CroppedBitmap _block_4 = null;
        private CroppedBitmap _block_5 = null;
        private CroppedBitmap _block_6 = null;
        private CroppedBitmap _block_7 = null;
        private CroppedBitmap _block_8 = null;

        /// <summary>
        /// 既定の初期値で <see cref="BlockImgHelper"/> の新しいインスタンスを作成します。
        /// </summary>
        public BlockImgHelper()
        {
            this.init_block_table();
            this.load_all_images();
        }

        public BlockImgHelper(Uri block_uri)
            : base()
        {
            this.block_img_uri = block_uri;
        }

        /// <summary>
        /// 色を指定してブロックを取得します。
        /// </summary>
        public Image GetBlock(BlockColors color)
        {
            if (color == BlockColors.Nothing)
            {
                return new Image()
                {
                    Source = block_img_table[BlockColors.None],
                };
            }
            else
            {
                // WPFに描画するためにオブジェクトへ設定する
                return new Image()
                {
                    Source = block_img_table[color],
                };
            }
        }

        /// <summary>
        /// 全てのブロックの画像を取得します。
        /// </summary>
        private void load_all_images()
        {
            BitmapImage src_img = this.load_src_img();

            for (int i = 0; i < this.block_img_table.Count; i++)
            {
                this.block_img_table[(BlockColors)i] =
                    this.load_single_image(src_img, 0, i * GameCondition.ImageHight);
            }
        }

        /// <summary>
        /// ソースの画像から指定座標が示すブロック画像を切り出しで取得します。
        /// </summary>
        private CroppedBitmap load_single_image(BitmapImage src_img, int x, int y)
        {
            var crppped_img = new CroppedBitmap();
            crppped_img.BeginInit();

            crppped_img.Source = src_img;

            // サイズの指定
            crppped_img.SourceRect =
                new Int32Rect(x, y, GameCondition.ImageWidth, GameCondition.ImageHight);

            crppped_img.EndInit();

            return crppped_img;
        }

        /// <summary>
        /// コンストラクタで指定されたURIから <see cref="BitmapImage"/> オブジェクトを取得します。
        /// </summary>
        private BitmapImage load_src_img()
        {
            var src_img = new BitmapImage();
            src_img.BeginInit();
            src_img.UriSource = this.block_img_uri;
            src_img.EndInit();
            return src_img;
        }

        /// <summary>
        /// このクラスが管理するイメージテーブルを初期化します。
        /// </summary>
        private void init_block_table()
        {
            this.block_img_table.Add(BlockColors.None, _block_1);
            this.block_img_table.Add(BlockColors.Red, _block_2);
            this.block_img_table.Add(BlockColors.Yellow, _block_3);
            this.block_img_table.Add(BlockColors.Green, _block_4);
            this.block_img_table.Add(BlockColors.Aqua, _block_5);
            this.block_img_table.Add(BlockColors.Blue, _block_6);
            this.block_img_table.Add(BlockColors.Purple, _block_7);
            this.block_img_table.Add(BlockColors.Orange, _block_8);
        }
    }

    /// <summary>
    /// ブロックの色を表す列挙型
    /// </summary>
    public enum BlockColors
    {
        Nothing = -1,
        None = 0,
        Red,
        Yellow,
        Green,
        Aqua,
        Blue,
        Purple,
        Orange,
    }

    /// <summary>
    /// ゲームの条件を表すクラス
    /// </summary>
    public class GameCondition
    {
        /// <summary>横幅のブロック数</summary>
        public const int Max_Block_Width = 10;
        /// <summary>縦幅のブロック数</summary>
        public const int Max_Block_Hight = 20;

        /// <summary>画像の横幅(px)</summary>
        public const int ImageWidth = 24;
        /// <summary>画像の縦(px)</summary>
        public const int ImageHight = 24;
    }

    /// <summary>
    /// ゲーム描画領域を表すクラス
    /// </summary>
    public class Bord
    {
        /// <summary>ブロック画像管理クラス</summary>
        private BlockImgHelper block_helper;

        /// <summary>テトリスのゲームボード(番兵込み)</summary>
        private BlockColors[,] bord = new BlockColors[GameCondition.Max_Block_Width + 2, GameCondition.Max_Block_Hight + 2 + 3];

        /// <summary>各操作で使用する乱数オブジェクト</summary>
        private Random r = new Random();

        /// <summary>
        /// 現在のブロックの状態を設定または取得します。
        /// </summary>
        public BlockStatus CurrentBlobkStatus { get; set; }

        /// <summary>
        /// 既定の初期値を用いて <see cref="Bord"/> クラスの新しいインスタンスを作成します。
        /// </summary>
        public Bord()
        {
            this.block_helper = new BlockImgHelper();
        }

        /// <summary>
        /// 描画対象のキャンバスオブジェクトを指定して現在の状態を描画します。
        /// </summary>
        public void ShowBord(Canvas canvas)
        {
            canvas.Children.Clear();
            for (int i = 0; i < GameCondition.Max_Block_Width; i++)
            {
                for (int j = 0; j < GameCondition.Max_Block_Hight; j++)
                {
                    Image img = this.block_helper.GetBlock(bord[i + 1, j + 1 + 3]);
                    Canvas.SetLeft(img, i * GameCondition.ImageWidth + 2);
                    Canvas.SetTop(img, j * GameCondition.ImageHight + 2);
                    canvas.Children.Add(img);
                }
            }
        }

        /// <summary>
        /// ボードの初期化
        /// </summary>
        public void InitBord()
        {
            for (int i = 0; i < GameCondition.Max_Block_Width + 2; i++)
            {
                for (int j = 0; j < GameCondition.Max_Block_Hight + 2 + 3; j++)
                {
                    if (i == 0 || i == 11 || j == 24)
                    {
                        this.bord[i, j] = BlockColors.Nothing;
                    }
                    else
                    {
                        this.bord[i, j] = BlockColors.None;
                    }
                }
            }
        }

        /// <summary>
        /// ブロックを1つ置きます
        /// </summary>
        public bool PutBlock(BlockStatus s, bool action = false)
        {
            if (this.bord[s.X, s.Y] != 0)
            {
                return false;
            }

            if (action)
            {
                bord[s.X, s.Y] = (BlockColors)s.Type;
            }

            for (int i = 0; i < 3; i++)
            {
                int dx = BlockDefine.Block[s.Type].Position[i].X;
                int dy = BlockDefine.Block[s.Type].Position[i].Y;
                int r = s.Rotate % BlockDefine.Block[s.Type].Rotate;
                for (int j = 0; j < r; j++)
                {
                    int nx = dx, ny = dy;
                    dx = ny;
                    dy = -nx;
                }
                if (this.bord[s.X + dx, s.Y + dy] != 0)
                {
                    return false;
                }
                if (action)
                {
                    this.bord[s.X + dx, s.Y + dy] = (BlockColors)s.Type;
                }
            }

            if (!action)
            {
                this.PutBlock(s, true);
            }
            return true;
        }

        /// <summary>
        /// ブロックを消去します。
        /// </summary>
        public bool DeleteBlock(BlockStatus s)
        {
            this.bord[s.X, s.Y] = BlockColors.None;
            for (int i = 0; i < 3; i++)
            {
                int dx = BlockDefine.Block[s.Type].Position[i].X;
                int dy = BlockDefine.Block[s.Type].Position[i].Y;
                int r = s.Rotate % BlockDefine.Block[s.Type].Rotate;
                for (int j = 0; j < r; j++)
                {
                    int nx = dx, ny = dy;
                    dx = ny;
                    dy = -nx;
                }
                this.bord[s.X + dx, s.Y + dy] = BlockColors.None;
            }
            return true;
        }

        /// <summary>
        /// 現在操作中のブロックを1つしたへ落とします。
        /// </summary>
        public bool DownCurrentBlock()
        {
            this.DeleteBlock(this.CurrentBlobkStatus);
            BlockStatus s = this.CurrentBlobkStatus.Clone();

            s.Y++;

            if (this.PutBlock(s))
            {
                this.CurrentBlobkStatus = s;
            }
            else
            {
                this.PutBlock(this.CurrentBlobkStatus);

                this.DeleteLine();

                BlockStatus new_block = this.CreateNewBlock();
                new_block = this.CreateNewBlock();
                //BlockStatus new_block = this.CreateNewBlock();
                if (this.PutBlock(new_block))
                {
                    this.CurrentBlobkStatus = new_block;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 新しいブロックを作成します。
        /// </summary>
        public BlockStatus CreateNewBlock()
        {
            return new BlockStatus()
            {
                X = 5,
                Y = 3,
                Type = (this.r.Next() % 7) +1,
                Rotate = this.r.Next(0, 4),
            };
        }

        /// <summary>
        /// 埋まっている行がある場合ブロックを消去して
        /// </summary>
        public void DeleteLine()
        {
            for (int y = 23; y >= 1; y--)
            {
                bool flag = true;
                for (int x = 1; x < 11; x++)
                {
                    if (bord[x, y] == BlockColors.None)
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    for (int j = y; j > 1; j--)
                    {
                        for (int i = 1; i < 11; i++)
                        {
                            this.bord[i, j] = this.bord[i, j - 1];
                        }
                    }
                    y++;
                }
            }
        }

        /// <summary>
        /// ゲームオーバーの表示を行います。
        /// </summary>
        public void GameOver()
        {
            for (int x = 1; x <= 11; x++)
            {
                for (int y = 0; y < 24; y++)
                {
                    if (this.bord[x, y] != BlockColors.None && this.bord[x, y] != BlockColors.Nothing)
                    {
                        this.bord[x, y] = BlockColors.Red;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 1つのブロックを表すクラス
    /// </summary>
    public class Block
    {
        public int Rotate;
        public Position[] Position = new Position[3];
        public Block()
        {
            this.Position = new Position[3]{
                new Position(),
                new Position(),
                new Position(),
            };
        }
    }

    /// <summary>
    /// ブロックの回転ごとの座標を表すクラス
    /// </summary>
    public class Position
    {
        public int X;
        public int Y;
        public Position() { }
    }

    /// <summary>
    /// ブロックの状態を表すクラス
    /// </summary>
    public class BlockStatus
    {
        public int X;
        public int Y;
        public int Type;
        public int Rotate;

        public BlockStatus Clone()
        {
            return new BlockStatus()
            {
                 X = this.X,
                 Y = this.Y,
                 Type = this.Type,
                 Rotate = this.Rotate,
            };
        }
    }

    /// <summary>
    /// ブロックの定義を行うクラス
    /// </summary>
    public class BlockDefine
    {
        /// <summary>ブロックの定義数(nullブロック込み)</summary>
        private const int block_cnt = 8;

        /// <summary>ブロック構造体</summary>
        public static Block[] Block = new Block[8];

        /// <summary>
        /// 既定の初期値を用いて <see cref="BlockDefine"/> の新しいインスタンスを作成します。
        /// </summary>
        static BlockDefine()
        {
            // null
            BlockDefine.Block[0] = new Block() { Rotate = 1 };
            BlockDefine.Block[0].Position[0].X = 0;
            BlockDefine.Block[0].Position[0].Y = 0;
            BlockDefine.Block[0].Position[1].X = 0;
            BlockDefine.Block[0].Position[1].Y = 0;
            BlockDefine.Block[0].Position[2].X = 0;
            BlockDefine.Block[0].Position[2].Y = 0;

            // tetoris
            BlockDefine.Block[1] = new Block() { Rotate = 2 };
            BlockDefine.Block[1].Position[0].X = 0;
            BlockDefine.Block[1].Position[0].Y = -1;
            BlockDefine.Block[1].Position[1].X = 0;
            BlockDefine.Block[1].Position[1].Y = 1;
            BlockDefine.Block[1].Position[2].X = 0;
            BlockDefine.Block[1].Position[2].Y = 2;

            // L1
            BlockDefine.Block[2] = new Block() { Rotate = 4 };
            BlockDefine.Block[2].Position[0].X = 0;
            BlockDefine.Block[2].Position[0].Y = -1;
            BlockDefine.Block[2].Position[1].X = 0;
            BlockDefine.Block[2].Position[1].Y = 1;
            BlockDefine.Block[2].Position[2].X = 1;
            BlockDefine.Block[2].Position[2].Y = 1;

            // L2
            BlockDefine.Block[3] = new Block() { Rotate = 4 };
            BlockDefine.Block[3].Position[0].X = 0;
            BlockDefine.Block[3].Position[0].Y = -1;
            BlockDefine.Block[3].Position[1].X = 0;
            BlockDefine.Block[3].Position[1].Y = 1;
            BlockDefine.Block[3].Position[2].X = -1;
            BlockDefine.Block[3].Position[2].Y = 1;

            // key1
            BlockDefine.Block[4] = new Block() { Rotate = 2 };
            BlockDefine.Block[4].Position[0].X = 0;
            BlockDefine.Block[4].Position[0].Y = -1;
            BlockDefine.Block[4].Position[1].X = 1;
            BlockDefine.Block[4].Position[1].Y = 0;
            BlockDefine.Block[4].Position[2].X = 1;
            BlockDefine.Block[4].Position[2].Y = 1;

            // key2
            BlockDefine.Block[5] = new Block() { Rotate = 2 };
            BlockDefine.Block[5].Position[0].X = 0;
            BlockDefine.Block[5].Position[0].Y = -1;
            BlockDefine.Block[5].Position[1].X = -1;
            BlockDefine.Block[5].Position[1].Y = 0;
            BlockDefine.Block[5].Position[2].X = -1;
            BlockDefine.Block[5].Position[2].Y = 1;

            // squere
            BlockDefine.Block[6] = new Block() { Rotate = 1 };
            BlockDefine.Block[6].Position[0].X = 0;
            BlockDefine.Block[6].Position[0].Y = 1;
            BlockDefine.Block[6].Position[1].X = 1;
            BlockDefine.Block[6].Position[1].Y = 0;
            BlockDefine.Block[6].Position[2].X = 1;
            BlockDefine.Block[6].Position[2].Y = 1;

            // T
            BlockDefine.Block[7] = new Block() { Rotate = 4 };
            BlockDefine.Block[7].Position[0].X = 0;
            BlockDefine.Block[7].Position[0].Y = -1;
            BlockDefine.Block[7].Position[1].X = 1;
            BlockDefine.Block[7].Position[1].Y = 0;
            BlockDefine.Block[7].Position[2].X = -1;
            BlockDefine.Block[7].Position[2].Y = 0;
        }
    }

    /// <summary>
    /// (キーボードイベントの発生が不連続のため)
    /// キーが押されっぱなしの状態を通知するためのクラス
    /// </summary>
    public class PeriodicSignals
    {
        /// <summary>キーが押され続けたときに発生する押しっぱなしイベントの発生周期</summary>
        DispatcherTimer timer;

        /// <summary>現在押下中のキー</summary>
        private Key key;

        /// <summary>
        /// 定期的に発生するキーイベントを設定または取得します
        /// </summary>
        public event Action<Key> Ticks;

        /// <summary>
        /// イベント発生間隔を指定してオブジェクトを初期化します。
        /// </summary>
        public PeriodicSignals(TimeSpan s)
        {
            this.timer = new DispatcherTimer()
            {
                Interval = s
            };
            this.timer.Tick += Tick;
        }

        /// <summary>
        /// 押されっぱなしの状態を開始します。
        /// </summary>
        public void Start(Key key)
        {
            if (this.key == key)
            {
                return; // 既にキーシグナル発行中
            }
            this.key = key;
            timer.Start();
            this.Ticks(key); // 最初の1回は即時呼び出し
        }

        /// <summary>
        /// キーが離された事をこのオブジェクトをへ通知します。
        /// </summary>
        public void Stop()
        {
            timer.Stop();
            this.key = Key.None;
        }

        /// <summary>
        /// Ticks イベントを発行します。
        /// </summary>
        public void Tick(object sender, EventArgs e)
        {
            if (this.Ticks == null)
            {
                return;
            }
            this.Ticks(this.key);
        }
    }
}