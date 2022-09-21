using System;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace Text_Caculator
{
    public partial class MainForm : Form
    {
        Font normalFont;
        Font italicFont;
        List<Label> labels;
        string[] prevLines;

        public MainForm()
        {
            InitializeComponent();

            normalFont = new Font("Cambria", 12F, FontStyle.Regular, GraphicsUnit.Point);
            italicFont = new Font("Cambria", 12F, FontStyle.Italic, GraphicsUnit.Point);
            labels = new List<Label>();
            prevLines = Array.Empty<string>();
        }

        #region Basic Textbox Functions
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Clear();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Redo();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.Paste(DataFormats.GetFormat("Text"));
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mainTextBox.SelectAll();
        }
        #endregion

        private void mainTextBox_TextChanged(object sender, EventArgs e)
        {
            int m_selectStart = mainTextBox.SelectionStart;
            int m_selectLength = mainTextBox.SelectionLength;
            mainTextBox.SuspendLayout();
            MyRichTextBox._Paint = false;

            int charIndex = 0;
            for (int lineIndex = 0; lineIndex < mainTextBox.Lines.Length; charIndex += mainTextBox.Lines[lineIndex].Length + 1, lineIndex++)
            {
                string? line = mainTextBox.Lines[lineIndex];

                Label label = lineIndex >= labels.Count ? CreateNewLabel() : labels[lineIndex];
                if (string.IsNullOrWhiteSpace(line))
                {
                    label.Visible = false;
                    continue;
                }

                if ((uint)lineIndex < (uint)prevLines.Length && prevLines[lineIndex] == line)
                    continue;

                mainTextBox.SelectionStart = charIndex;
                mainTextBox.SelectionLength = line.Length;
                mainTextBox.SelectionColor = SystemColors.WindowText;

                if (line.StartsWith("//"))
                {
                    mainTextBox.SelectionStart = charIndex;
                    mainTextBox.SelectionLength = line.Length;
                    mainTextBox.SelectionColor = Color.Green;
                    label.Visible = false;
                    continue;
                }

                label.Visible = true;
                string text;
                try
                {
                    var result = Evaluator.Evaluate(line);
                    if (result.isSuccessful)
                        text = $"= {result.value:G15}";
                    else
                    {
                        mainTextBox.SelectionStart = result.errorStartIndex + charIndex;
                        mainTextBox.SelectionLength = result.errorEndIndex - result.errorStartIndex;
                        mainTextBox.SelectionColor = Color.Red;
                        text = result.errorMessage;
                    }
                }
                catch (Exception ex)
                {
                    text = ex.Message + "\n" + ex.StackTrace;
                }

                label.Text = text;
                SetLabelLoc(label, charIndex + line.Length);
            }

            for (int i = mainTextBox.Lines.Length; i < labels.Count; i++)
                labels[i].Visible = false;

            mainTextBox.SelectionStart = m_selectStart;
            mainTextBox.SelectionLength = m_selectLength;
            MyRichTextBox._Paint = true;
            mainTextBox.ResumeLayout(false);

            prevLines = mainTextBox.Lines;
        }

        private void SetLabelLoc(Label label, int index)
        {
            var lblPos = mainTextBox.GetPositionFromCharIndex(index);
            lblPos.Offset(5, 0);
            label.Location = lblPos;
        }

        private Label CreateNewLabel()
        {
            Label label = new Label();
            label.ForeColor = Color.CadetBlue;
            label.BackColor = SystemColors.Control;
            label.Font = normalFont;
            label.AutoSize = true;
            label.TextAlign = ContentAlignment.MiddleLeft;
            labels.Add(label);
            mainTextBox.Controls.Add(label);
            return label;
        }

        private void mainTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (mainTextBox.SelectionLength > 0) return;

            if (mainTextBox.GetLineFromCharIndex(mainTextBox.SelectionStart) == 0 && e.KeyData == Keys.Up ||
                mainTextBox.GetLineFromCharIndex(mainTextBox.SelectionStart) == mainTextBox.GetLineFromCharIndex(mainTextBox.TextLength) && e.KeyData == Keys.Down ||
                mainTextBox.SelectionStart == mainTextBox.TextLength && e.KeyData == Keys.Right ||
                mainTextBox.SelectionStart == 0 && (e.KeyData == Keys.Left || e.KeyData == Keys.Back))
                e.Handled = true;
        }

        private void mainTextBox_VScroll(object sender, EventArgs e)
        {
            RepaintLabels();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            RepaintLabels();
        }

        private void RepaintLabels()
        {
            int charIndex = 0;
            for (int lineIndex = 0; lineIndex < mainTextBox.Lines.Length; charIndex += mainTextBox.Lines[lineIndex].Length + 1, lineIndex++)
                if (lineIndex < labels.Count)
                    SetLabelLoc(labels[lineIndex], charIndex + mainTextBox.Lines[lineIndex].Length);
        }
    }
}