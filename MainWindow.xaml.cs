using System;
using System.Windows;
using System.Windows.Controls;
using Gigasoft.ProEssentials;
using Gigasoft.ProEssentials.Enums;

namespace MultiAxisLayoutExplorer
{
    /// <summary>
    /// ProEssentials WPF — Multi-Axis Layout Explorer
    ///
    /// A single PesgoWpf chart displaying engine dyno data (HP, Torque,
    /// Temperature, Pressure vs RPM) across four instantly switchable
    /// axis layout modes. A toggle button moves Pressure to the right Y axis
    /// in whichever layout is active.
    ///
    /// LAYOUT MODES:
    ///   All Separate   — 4 independent stacked axes, one series each
    ///                    MultiAxesSubsets[0..3]=1, no OverlapMultiAxes
    ///                    (Examples 012 pattern)
    ///
    ///   All Overlapped — all 4 series share one Y region, each with its own scale
    ///                    MultiAxesSubsets[0..3]=1, OverlapMultiAxes[0]=4
    ///                    (Example 103 pattern)
    ///
    ///   2 + 2 Split    — HP+Torque overlapped on top, Temp+Pressure on bottom
    ///                    MultiAxesSubsets[0..3]=1, OverlapMultiAxes[0]=2,[1]=2
    ///                    (Example 104 pattern)
    ///
    ///   2 per Axis     — two axis sections, two series each
    ///                    MultiAxesSubsets[0]=2,[1]=2, no OverlapMultiAxes
    ///                    (Example 013 pattern)
    ///
    /// MIXING PLOTTING METHODS (Example 022 pattern):
    ///   PePlot.Methods[] (SGraphPlottingMethods enum) assigns a plotting
    ///   method per subset index. It is NOT WorkingAxis-dependent.
    ///   Adding OnRightAxis (1000) to any method routes that subset to the
    ///   right Y axis — no ComparisonSubsets or RYAxisComparisonSubsets needed.
    ///
    ///   ComparisonSubsets and RYAxisComparisonSubsets are a separate approach
    ///   that works with Method/MethodII. These two approaches must not be mixed.
    ///   This example uses Methods[] exclusively and never touches
    ///   ComparisonSubsets or RYAxisComparisonSubsets.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ── Subset colors — audio waveform dark palette ───────────────────
        static readonly System.Windows.Media.Color ColorHP     = System.Windows.Media.Color.FromArgb(255,   0, 229, 229); // cyan
        static readonly System.Windows.Media.Color ColorTorque = System.Windows.Media.Color.FromArgb(255,   0, 255,   0); // green
        static readonly System.Windows.Media.Color ColorTemp   = System.Windows.Media.Color.FromArgb(255, 255,  48,  48); // red
        static readonly System.Windows.Media.Color ColorPSI    = System.Windows.Media.Color.FromArgb(255, 255, 210,   0); // gold

        // SplineArea fill uses alpha so overlapping fills stay readable
        static readonly System.Windows.Media.Color ColorTorqueAlpha =
            System.Windows.Media.Color.FromArgb(160,   0, 255,   0);

        const int Points = 50;

        int  _currentLayout = 0;
        bool _ryActive      = false;

        Button _activeBtn;

        public MainWindow()
        {
            InitializeComponent();
            _activeBtn = BtnSeparate;
        }

        // -----------------------------------------------------------------------
        // Pesgo1_Loaded — chart initialization
        // -----------------------------------------------------------------------
        void Pesgo1_Loaded(object sender, RoutedEventArgs e)
        {
            // =======================================================================
            // Step 1 — Data: engine dyno sweep, 50 points, RPM 1000–6000
            //
            // DuplicateDataX = PointIncrement: all subsets share the same X values.
            // Only X[0, p] is stored; the chart duplicates it across all subsets.
            // =======================================================================
            Pesgo1.PeData.Subsets = 4;
            Pesgo1.PeData.Points  = Points;

            Pesgo1.PeData.DuplicateDataX = DuplicateData.PointIncrement;
            Pesgo1.PeData.X[0, Points - 1] = 0; // pre-allocate X array

            var rand = new Random(17);

            for (int p = 0; p < Points; p++)
            {
                float rpm = 1000f + p * (5000f / (Points - 1));
                float t   = (rpm - 1000f) / 5000f;

                Pesgo1.PeData.X[0, p] = rpm;

                // Horsepower — rises steeply, peaks ~5500 RPM
                Pesgo1.PeData.Y[0, p] = Math.Max(5f,
                    460f * (float)Math.Pow(t, 0.55) * (1f - 0.05f * (float)Math.Pow(1f - t, 3))
                    + (float)(rand.NextDouble() * 12 - 6));

                // Torque — bell curve peaking ~3800 RPM
                Pesgo1.PeData.Y[1, p] =
                    340f * (float)Math.Exp(-Math.Pow((t - 0.56f) / 0.3, 2))
                    + 55f + (float)(rand.NextDouble() * 10 - 5);

                // Temperature — near-linear rise, 165–275 F
                Pesgo1.PeData.Y[2, p] =
                    165f + t * 110f + (float)(rand.NextDouble() * 8 - 4);

                // Pressure — gentle arc peaking mid-range, 20–46 PSI
                Pesgo1.PeData.Y[3, p] =
                    20f + 26f * (float)Math.Sin(t * Math.PI)
                    + (float)(rand.NextDouble() * 4 - 2);
            }

            // =======================================================================
            // Step 2 — Subset labels and colors
            // =======================================================================
            Pesgo1.PeString.SubsetLabels[0] = "Horsepower (HP)";
            Pesgo1.PeString.SubsetLabels[1] = "Torque (lb-ft)";
            Pesgo1.PeString.SubsetLabels[2] = "Temperature (F)";
            Pesgo1.PeString.SubsetLabels[3] = "Pressure (PSI)";

            Pesgo1.PeColor.SubsetColors[0] = ColorHP;
            Pesgo1.PeColor.SubsetColors[1] = ColorTorqueAlpha;
            Pesgo1.PeColor.SubsetColors[2] = ColorTemp;
            Pesgo1.PeColor.SubsetColors[3] = ColorPSI;

            Pesgo1.PeLegend.SubsetLineTypes[0] = LineType.MediumSolid;
            Pesgo1.PeLegend.SubsetLineTypes[1] = LineType.MediumSolid;
            Pesgo1.PeLegend.SubsetLineTypes[2] = LineType.MediumSolid;
            Pesgo1.PeLegend.SubsetLineTypes[3] = LineType.MediumSolid;

            // =======================================================================
            // Step 3 — Titles, X axis label
            // =======================================================================
            Pesgo1.PeString.MainTitle  = "Engine Dyno — Multi-Axis Layout Explorer";
            Pesgo1.PeString.SubTitle   = "Switch layouts  -  toggle Pressure to right Y  -  drag separator to resize";
            Pesgo1.PeString.XAxisLabel = "RPM";

            // =======================================================================
            // Step 4 — Interaction and zoom
            // =======================================================================
            Pesgo1.PeUserInterface.Allow.Zooming   = AllowZooming.HorzAndVert;
            Pesgo1.PeUserInterface.Allow.ZoomStyle  = ZoomStyle.Ro2Not;
            Pesgo1.PeUserInterface.Allow.ZoomLimits = ZoomLimits.AxisHorizontal;

            Pesgo1.PeUserInterface.Scrollbar.ScrollingHorzZoom = true;
            Pesgo1.PeUserInterface.Scrollbar.ScrollingVertZoom = true;

            Pesgo1.PeUserInterface.Cursor.PromptTracking = true;
            Pesgo1.PeUserInterface.Cursor.PromptLocation = CursorPromptLocation.ToolTip;
            Pesgo1.PeUserInterface.Cursor.PromptStyle    = CursorPromptStyle.XYValues;

            Pesgo1.PePlot.MarkDataPoints = true;
            Pesgo1.PePlot.Option.MinimumPointSize   = MinimumPointSize.Small;
            Pesgo1.PePlot.Option.MaximumPointSize   = MinimumPointSize.Large;
            Pesgo1.PePlot.Option.SolidLineOverArea  = 1;
            Pesgo1.PePlot.Option.FixedLineThickness = true;

            // =======================================================================
            // Step 5 — Style
            // =======================================================================
            Pesgo1.PeColor.BitmapGradientMode = true;
            Pesgo1.PeColor.QuickStyle         = QuickStyle.DarkNoBorder;
            Pesgo1.PeColor.GridBold           = true;
            Pesgo1.PeConfigure.BorderTypes    = TABorder.DropShadow;

            Pesgo1.PeGrid.InFront     = true;
            Pesgo1.PeGrid.LineControl = GridLineControl.Both;
            Pesgo1.PeGrid.Style       = GridStyle.Dot;
            Pesgo1.PeGrid.GridBands   = false;
            Pesgo1.PePlot.DataShadows = DataShadows.Shadows;

            Pesgo1.PeFont.FontSize       = Gigasoft.ProEssentials.Enums.FontSize.Large;
            Pesgo1.PeFont.Fixed          = true;
            Pesgo1.PeFont.MainTitle.Bold = true;

            Pesgo1.PeConfigure.AntiAliasGraphics = true;
            Pesgo1.PeConfigure.RenderEngine      = RenderEngine.Direct2D;
            Pesgo1.PeConfigure.ImageAdjustLeft   = 25; // add 25/100 character width padding 
            Pesgo1.PeConfigure.ImageAdjustRight  = 25;

            // =======================================================================
            // Step 6 — Apply initial layout and render
            // =======================================================================
            ApplyLayout(0);
        }

        // -----------------------------------------------------------------------
        // ApplyMethods — sets PePlot.Methods[] for all 4 subsets
        //
        // PePlot.Methods[] is NOT WorkingAxis-dependent — it is a global
        // per-subset array. It must never be mixed with ComparisonSubsets or
        // RYAxisComparisonSubsets, which belong to the separate Method/MethodII
        // approach.
        //
        // OnRightAxis (1000) added to subset 3 routes Pressure to the right Y
        // axis when _ryActive — this is the only right-Y mechanism used here.
        // Cast to (int) is required when adding enum values together.
        // -----------------------------------------------------------------------
        void ApplyMethods()
        {
            Pesgo1.PePlot.Methods[0] = SGraphPlottingMethods.Bar;            // HP
            Pesgo1.PePlot.Methods[1] = SGraphPlottingMethods.SplineArea;     // Torque
            Pesgo1.PePlot.Methods[2] = SGraphPlottingMethods.PointsPlusLine; // Temp
            Pesgo1.PePlot.Methods[3] = _ryActive
                ? SGraphPlottingMethods.Spline + (int)SGraphPlottingMethods.OnRightAxis
                : SGraphPlottingMethods.Spline;                              // Pressure
        }

        // -----------------------------------------------------------------------
        // ApplyLayout — reshapes the entire axis configuration
        //
        // Clears multi-axis arrays, picks the layout, applies per-axis config,
        // calls ApplyMethods(), then ReinitializeResetImage to render atomically.
        // -----------------------------------------------------------------------
        void ApplyLayout(int layout)
        {
            _currentLayout = layout;

            // .Clear() empties all elements in each array in one call,
            // preventing stale values from a previous layout bleeding through.
            Pesgo1.PeGrid.MultiAxesSubsets.Clear();
            Pesgo1.PeGrid.OverlapMultiAxes.Clear();
            Pesgo1.PeGrid.MultiAxesProportions.Clear();

            switch (layout)
            {
                case 0: Layout_AllSeparate();   break;
                case 1: Layout_AllOverlapped(); break;
                case 2: Layout_Split2x2();      break;
                case 3: Layout_TwoPerAxis();    break;
            }

            // Apply plotting methods after axis config (not WorkingAxis-dependent)
            ApplyMethods();

            // Always reset WorkingAxis to 0 when done configuring axes
            Pesgo1.PeGrid.WorkingAxis = 0;

            Pesgo1.PeFunction.ReinitializeResetImage();
            Pesgo1.Invalidate();
        }

        // -----------------------------------------------------------------------
        // Layout 0 — All Separate (012 pattern)
        // -----------------------------------------------------------------------
        void Layout_AllSeparate()
        {
            Pesgo1.PeGrid.MultiAxesSubsets[0] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[1] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[2] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[3] = 1;

            Pesgo1.PeGrid.MultiAxesProportions[0] = 0.25f;
            Pesgo1.PeGrid.MultiAxesProportions[1] = 0.25f;
            Pesgo1.PeGrid.MultiAxesProportions[2] = 0.25f;
            Pesgo1.PeGrid.MultiAxesProportions[3] = 0.25f;

            Pesgo1.PeGrid.Option.MultiAxisStyle      = MultiAxisStyle.SeparateAxes;
            Pesgo1.PeGrid.Option.MultiAxesSeparators = MultiAxesSeparators.Medium;
            Pesgo1.PeUserInterface.Allow.MultiAxesSizing = true;

            ConfigureAxes_4Individual();
        }

        // -----------------------------------------------------------------------
        // Layout 1 — All Overlapped (103 pattern)
        // -----------------------------------------------------------------------
        void Layout_AllOverlapped()
        {
            Pesgo1.PeGrid.MultiAxesSubsets[0] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[1] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[2] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[3] = 1;

            Pesgo1.PeGrid.OverlapMultiAxes[0] = 4;
            Pesgo1.PeGrid.MultiAxesProportions[0] = 1.0f;

            Pesgo1.PeGrid.Option.MultiAxisStyle    = MultiAxisStyle.GroupAllAxes;
            Pesgo1.PeGrid.Option.AxisNumberSpacing = 2.0;
            Pesgo1.PeUserInterface.Allow.MultiAxesSizing = false;

            ConfigureAxes_4Individual();
        }

        // -----------------------------------------------------------------------
        // Layout 2 — 2 + 2 Split (104 pattern)
        // -----------------------------------------------------------------------
        void Layout_Split2x2()
        {
            Pesgo1.PeGrid.MultiAxesSubsets[0] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[1] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[2] = 1;
            Pesgo1.PeGrid.MultiAxesSubsets[3] = 1;

            Pesgo1.PeGrid.OverlapMultiAxes[0] = 2; // axes 0+1 share top region
            Pesgo1.PeGrid.OverlapMultiAxes[1] = 2; // axes 2+3 share bottom region
            Pesgo1.PeGrid.MultiAxesProportions[0] = 0.5f;
            Pesgo1.PeGrid.MultiAxesProportions[1] = 0.5f;

            Pesgo1.PeGrid.Option.MultiAxisStyle      = MultiAxisStyle.GroupAllAxes;
            Pesgo1.PeGrid.Option.MultiAxesSeparators = MultiAxesSeparators.Medium;
            Pesgo1.PeUserInterface.Allow.MultiAxesSizing = true;

            ConfigureAxes_4Individual();
        }

        // -----------------------------------------------------------------------
        // Layout 3 — 2 per Axis (013 pattern)
        //
        // Two axis sections, two subsets each:
        //   Axis 0: HP (subset 0) + Torque (subset 1)
        //   Axis 1: Temp (subset 2) + Pressure (subset 3)
        //
        // Right Y routing is handled entirely by Methods[3] + OnRightAxis in
        // ApplyMethods(). RYAxisLabel is set here when _ryActive to label the
        // right Y scale that OnRightAxis creates.
        // -----------------------------------------------------------------------
        void Layout_TwoPerAxis()
        {
            Pesgo1.PeGrid.MultiAxesSubsets[0] = 2; // axis 0: subsets 0+1
            Pesgo1.PeGrid.MultiAxesSubsets[1] = 2; // axis 1: subsets 2+3

            Pesgo1.PeGrid.MultiAxesProportions[0] = 0.5f;
            Pesgo1.PeGrid.MultiAxesProportions[1] = 0.5f;

            Pesgo1.PeGrid.Option.MultiAxisStyle      = MultiAxisStyle.SeparateAxes;
            Pesgo1.PeGrid.Option.MultiAxesSeparators = MultiAxesSeparators.Medium;
            Pesgo1.PeUserInterface.Allow.MultiAxesSizing = true;

            // Axis 0 — HP + Torque
            Pesgo1.PeGrid.WorkingAxis = 0;
            Pesgo1.PeColor.YAxis        = ColorHP;
            Pesgo1.PeString.YAxisLabel  = "HP / Torque";
            Pesgo1.PeString.RYAxisLabel = "";

            // Axis 1 — Temp + Pressure
            Pesgo1.PeGrid.WorkingAxis = 1;
            Pesgo1.PeColor.YAxis       = ColorTemp;
            Pesgo1.PeString.YAxisLabel = "Temp (F)";

            if (_ryActive)
            {
                Pesgo1.PeColor.RYAxis       = ColorPSI;
                Pesgo1.PeString.RYAxisLabel  = "Pressure (PSI)";
            }
            else
            {
                Pesgo1.PeString.RYAxisLabel  = "";
            }

            Pesgo1.PeLegend.Style = LegendStyle.OneLineTopOfAxis;
        }

        // -----------------------------------------------------------------------
        // ConfigureAxes_4Individual — per-axis setup for layouts 0, 1, and 2
        //
        // Each subset has its own WorkingAxis (0=HP, 1=Torque, 2=Temp, 3=Pressure).
        // Axis color syncs to subset color — critical in overlapped mode.
        // RYAxisLabel is set on WorkingAxis=3 when _ryActive to label the right
        // Y axis that Methods[3]+OnRightAxis creates in ApplyMethods().
        // -----------------------------------------------------------------------
        void ConfigureAxes_4Individual()
        {
            Pesgo1.PeGrid.WorkingAxis = 0;
            Pesgo1.PeColor.YAxis        = ColorHP;
            Pesgo1.PeString.YAxisLabel  = "HP";
            Pesgo1.PeString.RYAxisLabel = "";

            Pesgo1.PeGrid.WorkingAxis = 1;
            Pesgo1.PeColor.YAxis        = ColorTorque;
            Pesgo1.PeString.YAxisLabel  = "Torque (lb-ft)";
            Pesgo1.PeString.RYAxisLabel = "";

            Pesgo1.PeGrid.WorkingAxis = 2;
            Pesgo1.PeColor.YAxis        = ColorTemp;
            Pesgo1.PeString.YAxisLabel  = "Temp (F)";
            Pesgo1.PeString.RYAxisLabel = "";

            Pesgo1.PeGrid.WorkingAxis = 3;
            Pesgo1.PeColor.YAxis       = ColorPSI;
            Pesgo1.PeString.YAxisLabel = "Pressure (PSI)";

            if (_ryActive)
            {
                Pesgo1.PeColor.RYAxis       = ColorPSI;
                Pesgo1.PeString.RYAxisLabel  = "Pressure (PSI)";
            }
            else
            {
                Pesgo1.PeString.RYAxisLabel  = "";
            }

            Pesgo1.PeLegend.Style = LegendStyle.OneLineTopOfAxis;
        }

        // -----------------------------------------------------------------------
        // Button handlers
        // -----------------------------------------------------------------------
        void SetActiveButton(Button btn)
        {
            _activeBtn.Style = (Style)FindResource("LayoutBtn");
            _activeBtn       = btn;
            btn.Style        = (Style)FindResource("LayoutBtnActive");
        }

        private void BtnSeparate_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnSeparate);
            ApplyLayout(0);
        }

        private void BtnOverlapped_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnOverlapped);
            ApplyLayout(1);
        }

        private void BtnSplit_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnSplit);
            ApplyLayout(2);
        }

        private void BtnTwoPerAxis_Click(object sender, RoutedEventArgs e)
        {
            SetActiveButton(BtnTwoPerAxis);
            ApplyLayout(3);
        }

        private void BtnRY_Click(object sender, RoutedEventArgs e)
        {
            _ryActive = !_ryActive;
            BtnRY.Style = _ryActive
                ? (Style)FindResource("ToggleOnBtn")
                : (Style)FindResource("LayoutBtn");
            ApplyLayout(_currentLayout);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
