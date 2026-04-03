using System.Windows.Controls;

namespace POPSManager.Views
{
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        public void UpdateStats(int processed, int pending, int errors)
        {
            ProcessedCount.Text = processed.ToString();
            PendingCount.Text = pending.ToString();
            ErrorCount.Text = errors.ToString();
        }

        public void AddActivity(string message)
        {
            RecentActivity.Items.Insert(0, message);

            if (RecentActivity.Items.Count > 50)
                RecentActivity.Items.RemoveAt(RecentActivity.Items.Count - 1);
        }
    }
}
