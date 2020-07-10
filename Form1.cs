using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace PW_zadanie_4_1
{
    public partial class Form1 : Form
    {
        private static bool bGoing = false;
        public static bool bInvalidUserInput = false;
        public static bool bPrintOutput;
        public static string lastWarning = "";
        private static int lp = 0;
        private static int refreshTime = 200;
        private static Thread monitorThread, computationThread;
        private delegate void SafeCallDelegate(int n);
        private delegate void SafeCallDelegate2(List<byte>list, int lp);
        private delegate void SafeCallDelegate3();
        private delegate void SafeCallDelegate4(int curr, int NbPerms);

        public Form1()
        {
            InitializeComponent();
        }

        private byte GetNbElements()
        {
            string text = maskedTextBox1.Text;
            try
            {
                if (Convert.ToInt32(text) > 12 || text.Length > 3 || text.Length == 0)
                {
                    bInvalidUserInput = true;
                    bGoing = false;
                    lastWarning = "Podano zbyt duże n!";
                    return 0;
                }
            }
            catch(System.FormatException)
            {
                bInvalidUserInput = true;
                bGoing = false;
                lastWarning = "Nie podano n!";
                return 0;
            }
            bInvalidUserInput = false;
            lastWarning = "";
            return Convert.ToByte(text);
        }

        private void ShowWarning()
        {
            warningLabel.Text = lastWarning;
            warningLabel.Visible = true;
        }
        
        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!bGoing)
            {
                progressLabel.Visible = false;
                richTextBox1.SelectAll();
                richTextBox1.Text = "";

                bGoing = true;
                byte n = GetNbElements();
                if (n > 10)
                {
                    bPrintOutput = false;
                    richTextBox1.Text = "Wyświetlanie permutacji jest pomijane ze względu na ich liczbę.";
                }
                else bPrintOutput = true;
                if (bInvalidUserInput)
                {
                    ShowWarning();
                    bGoing = false;
                    return;
                }
                warningLabel.Visible = false;
                monitorThread = new Thread(UpdateProgressInfos);
                computationThread = new Thread(ComputePerms);
                monitorThread.Start(n);
                computationThread.Start(n);
            }
        }
        private static int Factorial(int n)
        {
            if (n == 0) return 1;
            int result = 1;
            for (int i = 1; i <= n; i++) result *= i;
            return result;
        }

        private void ComputePerms(object elementsCount)
        {
            byte n = (byte)elementsCount;
            InitProgressBar(Factorial(n));
            GeneratePerms(n);
            computationThread.Join();
        }

        private void InitProgressBar(int max)
        {
            if(progressBar1.InvokeRequired)
            {
                var d = new SafeCallDelegate(InitProgressBar);
                progressBar1.Invoke(d, new object[] { max });
            }
            else 
            {
                progressBar1.Visible = true;
                progressBar1.Value = 1;
                progressBar1.Minimum = 1;
                progressBar1.Maximum = max;
                progressBar1.Step = 1;
            }
        }

        private void UpdateProgressInfos(object elementsCount)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            TimeSpan elapsed = new TimeSpan();
            byte n = (byte)elementsCount;
            int NbPerms = Factorial(n);
            SetNbPerms(NbPerms);
            int currNbPerms = lp + 1;
            while (bGoing)
            {
                stopwatch.Stop();
                elapsed += stopwatch.Elapsed;
                stopwatch.Restart();
                int timeLeft;
                if(bPrintOutput)
                { 
                    timeLeft = (int)CalcRemainingTime(currNbPerms, NbPerms, elapsed);
                    UpdateRemainingTime(timeLeft);
                }
                else UpdateProgress(lp, NbPerms);
                Thread.Sleep(refreshTime);
            }
            UpdateRemainingTime(-1);
            stopwatch.Stop();
            monitorThread.Join();
        }
        private void UpdateProgress(int curr, int NbPerms)
        {
            if (progressLabel.InvokeRequired)
            {
                var d = new SafeCallDelegate4(UpdateProgress);
                progressLabel.Invoke(d, new object[] { curr, NbPerms });
            }
            else
            {
                progressLabel.Visible = true;
                float truncated = (float)(Math.Truncate((double)curr / NbPerms * 1000.0) / 1000.0);
                progressLabel.Text = truncated.ToString() + "%";
            }
        }
        private double CalcRemainingTime( int currPerms, int totalPerms, TimeSpan currTime) //in seconds
        {
            double seconds = currTime.Days * 86400 + currTime.Hours * 3600 + currTime.Minutes * 60 + currTime.Seconds + (double)currTime.Milliseconds / 1000;
            double speed = currPerms / seconds;
            double remainingTime = (totalPerms - currPerms) / speed;
            return Math.Round(remainingTime);
        }

        private void UpdateRemainingTime(int timeLeft)
        { 
            if(progressLabel.InvokeRequired)
            {
                var d = new SafeCallDelegate(UpdateRemainingTime);
                try {progressLabel.Invoke(d, new object[] { timeLeft }); }
                catch(System.ComponentModel.InvalidAsynchronousStateException) { computationThread.Join(); }
            }
            else
            {
                progressLabel.Visible = true;
                if (timeLeft > 59) progressLabel.Text = "Pozostało: " + (timeLeft / 60).ToString() + "min " + (timeLeft % 60).ToString() + "s";
                else if (timeLeft > 4) progressLabel.Text = "Pozostało: " + timeLeft.ToString() + "s";
                else if (timeLeft > 1) progressLabel.Text = "Pozostały: " + timeLeft.ToString() + "s";
                else if (timeLeft == 1) progressLabel.Text = "Pozostała: 1s";
                else if (timeLeft == -1) progressLabel.Text = "Ukończono";
                else progressLabel.Text = "Kończenie...";
            }
        }

        private void SetNbPerms(int nb)
        {
            if(textBox1.InvokeRequired)
            {
                var d = new SafeCallDelegate(SetNbPerms);
                textBox1.Invoke(d, new object[] { nb });
            }
            else textBox1.Text = nb.ToString();

        }
        //sequential Heap's algorithm
        private void GeneratePerms(int n)
        {
            List<byte> pseudoStack = new List<byte>();
            for (byte i = 0; i < n; i++) { pseudoStack.Add(0); }
            List<byte> permSet = new List<byte>();
            for (byte i = 0; i < n; i++) { permSet.Add(i); }
            if (bPrintOutput)
            {
                PrintPerm(permSet, lp);
                lp++;
            }
            byte stackIndex = 0;
            bool bPermFound;
            while(stackIndex < n)
            {
                bPermFound = false;
                if(pseudoStack[stackIndex] < stackIndex)
                {
                    if (stackIndex % 2 == 0) Swap(permSet, 0, stackIndex);
                    else Swap(permSet, pseudoStack[stackIndex], stackIndex);
                    if (bPrintOutput){ PrintPerm(permSet, lp);}
                    lp++;
                    bPermFound = true;
                    pseudoStack[stackIndex]++;
                    stackIndex = 0;
                }
                else
                {
                    pseudoStack[stackIndex]=0;
                    stackIndex++;
                }
                if (bPermFound) UpdateProgressBar(); 
            }
            bGoing = false;
            lp = 0;
        }

        private void UpdateProgressBar()
        {
            if(progressBar1.InvokeRequired)
            {
                var d = new SafeCallDelegate3(UpdateProgressBar);
                try { progressBar1.Invoke(d); }
                catch (System.ComponentModel.InvalidAsynchronousStateException) { computationThread.Join(); }
            }
            else progressBar1.PerformStep();
        }

        public void Swap(List<byte>list, byte indexA, byte indexB)
        {
            byte tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        private void PrintPerm(List<byte> permSet, int lp)
        {
            string perm = lp.ToString() + ".\t[ ";
            foreach (byte element in permSet) { perm += element.ToString() + " "; }
            perm += "]\n";
            if (richTextBox1.InvokeRequired)
            {
                var d = new SafeCallDelegate2(PrintPerm);
                try { richTextBox1.Invoke(d, new object[] { permSet, lp }); }
                catch (System.ComponentModel.InvalidAsynchronousStateException) { computationThread.Join(); }
            }
            else richTextBox1.AppendText(perm);
        }
    }
}
