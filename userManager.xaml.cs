using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OneDriveToolls
{
    /// <summary>
    /// userManager.xaml 的交互逻辑
    /// </summary>
    public partial class userManager : Window
    {
        public userManager()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (conf.AppSettings.Settings.AllKeys.Where(a => a == cmbClientID.Text).Any()) {
                string[] users = conf.AppSettings.Settings[cmbClientID.Text].Value.Split(',');
                if (users.Where(a => a == cmbUsername.Text).Any()) {
                    MessageBox.Show("账号已存在");
                    return;
                }
            }

            conf.AppSettings.Settings.Add(cmbClientID.Text, cmbUsername.Text);
            conf.Save();
                      
        }
    }
}
