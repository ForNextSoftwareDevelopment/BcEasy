using BcEasy.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BcEasy
{
    public partial class MainForm : Form
    {
        #region Members

        // Current folder
        private string currentfolder;

        // All folders in current folder 
        private List<string> folders;

        // All files in current folder 
        private List<string> files;

        // Full screen image box
        private PictureBox pictureBox;

        // Image for full screen
        private Image image;

        // Image panning
        private bool panning = false;
        private Point startingPoint = Point.Empty;
        private Point movingPoint = Point.Empty;
        private Point imagePoint = Point.Empty;

        // Zoom factor
        private float zoom = 1.0f;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            currentfolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            FillFolders(currentfolder);
        }

        #endregion

        #region EventHandlers

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '+')
            {
                Font font = new Font(lbFolders.Font.FontFamily, lbFolders.Font.Size + 1);
                lbFolders.Font = font;
                lbFiles.Font = font;
            }

            if (e.KeyChar == '-')
            {
                Font font = new Font(lbFolders.Font.FontFamily, lbFolders.Font.Size - 1);
                lbFolders.Font = font;
                lbFiles.Font = font;
            }
        }

        /// <summary>
        /// Folder selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            pbPreview.Image = Resources.noimage;

            string selected = lbFolders.SelectedItem.ToString();
            if (selected == "..")
            {
                FillFiles(currentfolder + "\\");
            } else if (selected.Contains(":"))
            {
                FillFiles(selected + "\\");
            } else
            {
                FillFiles(currentfolder + "\\" + selected);
            }
        }

        /// <summary>
        /// Double click on folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFolders_DoubleClick(object sender, EventArgs e)
        {
            string selectedfolder = "";

            string selected = lbFolders.SelectedItem.ToString();
            if (selected == "..")
            {
                string[] temp = currentfolder.Split(new char[] { '\\' });
                currentfolder = temp[0];
                selectedfolder = temp[temp.Length - 1];
                for (int i = 1; i < temp.Length - 1; i++)
                {
                    currentfolder += "\\" + temp[i];
                }
            } else if (selected.Contains(":"))
            {
                currentfolder = selected + "\\";
            } else
            {
                currentfolder += "\\" + selected;
            }

            FillFolders(currentfolder);

            if (selectedfolder != "")
            {
                for (int i = 0; i < lbFolders.Items.Count; i++)
                {
                    if (lbFolders.Items[i].ToString() == selectedfolder)
                    {
                        lbFolders.SelectedIndex = i;
                    }
                }
            }
        }

        /// <summary>
        /// Key pressed, check for enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFolders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                lbFolders_DoubleClick(sender, null);
            }
        }

        /// <summary>
        /// Image selected, show in preview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = lbFolders.SelectedItem.ToString();
            string path;
            if (selected == "..")
            {
                path = currentfolder + "\\" + lbFiles.SelectedItem.ToString();
            } else
            {
                path = currentfolder + "\\" + selected + "\\" + lbFiles.SelectedItem.ToString();
            }

            ShowPreview(path); 
        }

        /// <summary>
        /// Key pressed in files listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Enter) && (lbFiles.SelectedItem != null))
            {
                string file = lbFiles.SelectedItem.ToString();
                ShowFullSize(file);
            }
        }

        /// <summary>
        /// Show full sized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string file = lbFiles.SelectedItem.ToString();
            ShowFullSize(file);
        }

        /// <summary>
        /// Draw image in fullscreen mode (panning)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pictureBox = (PictureBox)sender;
            if (pictureBox.SizeMode == PictureBoxSizeMode.CenterImage)
            {
                e.Graphics.Clear(Color.Black);

                int startX = (int)(imagePoint.X + movingPoint.X);
                int startY = (int)(imagePoint.Y + movingPoint.Y);

                e.Graphics.DrawImage(image, startX, startY, image.Size.Width * zoom, image.Size.Height * zoom);
            } else
            {
                e.Graphics.Clear(Color.Black);
                
                float q1 = (float)image.Size.Width / (float)pictureBox.Size.Width;
                float q2 = (float)image.Size.Height / (float)pictureBox.Size.Height;
                float q = 1;
                int startX = 0;
                int startY = 0;

                if ((q1 > 1) || (q2 > 1))
                {
                    if (q1 >= q2) q = q1;
                    if (q1 < q2) q = q2;

                    startX = (int)((pictureBox.Width / 2) - (image.Width / 2 / q));
                    startY = (int)((pictureBox.Height / 2) - (image.Height / 2 / q));
                } else
                {
                    if (q1 <= q2) q = q1;
                    if (q1 < q2) q = q2;

                    startX = (int)((pictureBox.Width / 2) - (image.Width / 2 / q));
                    startY = (int)((pictureBox.Height / 2) - (image.Height / 2 / q));
                }

                imagePoint = new Point(startX, startY);
                e.Graphics.DrawImage(image, imagePoint.X, imagePoint.Y, (float)image.Size.Width / q, (float)image.Size.Height / q);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Fill folder listbox
        /// </summary>
        /// <param name="folder"></param>
        private void FillFolders(string folder)
        {
            folders = new List<string>();
            folders.Add("..");

            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    folders.Add(d.Name.TrimEnd(new char[] {'/'}));
                }

                string[] temp = Directory.GetDirectories(folder + "\\", "*", SearchOption.TopDirectoryOnly);
                Array.Sort(temp);
                for (int i = 0; i < temp.Length; i++)
                {
                    DirectoryInfo di = new DirectoryInfo(temp[i]);
                    if (
                        ((di.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint) &&
                        ((di.Attributes & FileAttributes.System) != FileAttributes.System)
                       )
                    {
                        string[] split = temp[i].Split(new char[] { '\\' });
                        folders.Add(split[split.Length - 1]);
                    }
                }
            } catch (Exception ex)
            {
                MessageBox.Show(this, "Error getting folders:" + ex, "EXCEPTION", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            lbFolders.DataSource = folders;
        }

        /// <summary>
        /// Fill files listbox
        /// </summary>
        /// <param name="folder"></param>
        private void FillFiles(string folder)
        {
            files = new List<string>();

            try
            {
                string[] temp;
                
                temp = Directory.GetFiles(folder + "\\", "*.jpg", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }

                temp = Directory.GetFiles(folder + "\\", "*.jpeg", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }

                temp = Directory.GetFiles(folder + "\\", "*.png", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }

                temp = Directory.GetFiles(folder + "\\", "*.tif", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }

                temp = Directory.GetFiles(folder + "\\", "*.tiff", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }

                temp = Directory.GetFiles(folder + "\\", "*.bmp", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < temp.Length; i++)
                {
                    string[] split = temp[i].Split(new char[] { '\\' });
                    files.Add(split[split.Length - 1]);
                }
            } catch (Exception ex)
            {
                MessageBox.Show(this, "Error getting files:" + ex, "EXCEPTION", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            files.Sort();
            lbFiles.DataSource = files;
        }

        /// <summary>
        /// Show preview of selected image
        /// </summary>
        /// <param name="path"></param>
        private void ShowPreview(string path)
        {
            try
            { 
                Image image = new Bitmap(path);
                pbPreview.Image = image;
            } catch (Exception ex)
            {
                pbPreview.Image = Resources.noimage;
            }
        }

        /// <summary>
        /// Show image fullsize
        /// </summary>
        /// <param name="file"></param>
        private void ShowFullSize(string file)
        {
            Form form = new Form();
            form.BackColor = Color.Black;
            form.Icon = Resources.icon;
            
            zoom = 1.0f;

            string path;
            string selected = lbFolders.SelectedItem.ToString();
            if (selected == "..")
            {
                path = currentfolder + "\\";
            } else
            {
                path = currentfolder + "\\" + selected + "\\";
            }

            try
            {
                image = Image.FromFile(path + file);
                pictureBox = new PictureBox();
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Image = image;
                pictureBox.Paint += PictureBox_Paint;

                Cursor.Hide();

                pictureBox.MouseDoubleClick += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left) form.Close();
                    for (int i=0; i< lbFiles.Items.Count; i++)
                    {
                        if (lbFiles.Items[i].ToString() == file)
                        {
                            lbFiles.SelectedIndex = i;
                        }
                    }

                    Cursor.Show();
                };

                pictureBox.MouseWheel += (sender, e) =>
                {
                    if (pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
                    {
                        int next = 0;
                        if (e.Delta < 0)
                        {
                            for (int i = 0; i < files.Count; i++)
                            {
                                if (files[i] == file) next = i + 1;
                            }

                            if (next < files.Count)
                            {
                                file = files[next];
                            }
                        }

                        if (e.Delta > 0)
                        {
                            for (int i = 0; i < files.Count; i++)
                            {
                                if (files[i] == file) next = i - 1;
                            }

                            if (next >= 0)
                            {
                                file = files[next];
                            }
                        }

                        try
                        {
                            image = Image.FromFile(path + file);
                            pictureBox.Image = image;
                            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        } catch (Exception ex)
                        {
                            pictureBox.Image = Resources.noimage;
                        }
                    } else 
                    {
                        if ((e.Delta < 0) && (zoom > 0.1))
                        {
                            zoom -= 0.1f;
                            movingPoint = new Point((int)(movingPoint.X + 0.05 * image.Width), (int)(movingPoint.Y + 0.05 * image.Height));
                        }

                        if ((e.Delta > 0) && (zoom < 10))
                        {
                            zoom += 0.1f;
                            movingPoint = new Point((int)(movingPoint.X - 0.05 * image.Width), (int)(movingPoint.Y - 0.05 * image.Height));
                        }

                        pictureBox.Invalidate();
                    }
                };

                pictureBox.MouseDown += (sender, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        if (pictureBox.SizeMode == PictureBoxSizeMode.Zoom)
                        {
                            zoom = 1.0f;
                            imagePoint = new Point((form.Width / 2) - (image.Width / 2), (form.Height / 2) - (image.Height / 2));
                            startingPoint = Point.Empty;
                            movingPoint = Point.Empty;
                            pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                        } else if (pictureBox.SizeMode == PictureBoxSizeMode.CenterImage)
                        {
                            zoom = 1.0f;
                            imagePoint = new Point((form.Width / 2) - (image.Width / 2), (form.Height / 2) - (image.Height / 2));
                            startingPoint = Point.Empty;
                            movingPoint = Point.Empty;
                            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                        }
                    }

                    if ((e.Button == MouseButtons.Left) && (pictureBox.SizeMode == PictureBoxSizeMode.CenterImage))
                    {
                        panning = true;
                        imagePoint = new Point(imagePoint.X + movingPoint.X, imagePoint.Y + movingPoint.Y);
                        movingPoint = new Point(e.X, e.Y);
                        startingPoint = movingPoint;
                        Cursor.Current = Cursors.Hand;
                        Cursor.Show();
                    }
                };

                pictureBox.MouseUp += (sender, e) =>
                {
                    if (panning)
                    {
                        panning = false;
                        Cursor.Current = Cursors.Arrow;
                        Cursor.Hide();
                    }
                };

                pictureBox.MouseMove += (sender, e) =>
                {
                    if (panning)
                    {
                        movingPoint = new Point(e.Location.X - startingPoint.X, e.Location.Y - startingPoint.Y);
                        pictureBox.Invalidate();
                    }
                };
               
                form.KeyDown += (sender, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        form.Close();
                        Cursor.Show();
                    }
                };

                form.Controls.Add(pictureBox);
                form.WindowState = FormWindowState.Maximized;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Show();
            } catch (Exception ex)
            {
                pbPreview.Image = Resources.noimage;
            }
        }

        #endregion
    }
}
