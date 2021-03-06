﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Schoology_Simplified
{
    public partial class LoginPage : Form
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void GoButtonClick(object sender, EventArgs e)
        {
            button1.Enabled = false;
            label5.Visible = false;

            textBox1.Enabled = false;
            textBox2.Enabled = false;

            radioButton1.Enabled = false;
            radioButton2.Enabled = false;
            radioButton3.Enabled = false;
            radioButton4.Enabled = false;
            radioButton5.Enabled = false;

            new Thread(async () =>
            {
                string username = textBox1.Text;
                string password = textBox2.Text;
                string grade = "";

                if (radioButton1.Checked)
                {
                    grade = "9AB";
                } 
                else if (radioButton2.Checked)
                {
                    grade = "9BB";
                } 
                else if (radioButton3.Checked)
                {
                    grade = "10AB";
                } 
                else if (radioButton4.Checked)
                {
                    grade = "11AB";
                }
                else if (radioButton5.Checked)
                {
                    grade = "12AB";
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        button1.Enabled = true;

                        textBox1.Enabled = true;
                        textBox2.Enabled = true;

                        radioButton1.Enabled = true;
                        radioButton2.Enabled = true;
                        radioButton3.Enabled = true;
                        radioButton4.Enabled = true;
                        radioButton5.Enabled = true;

                        label5.Text = "Please choose your grade.";

                        this.label5.Location = new System.Drawing.Point(295, 511);
                        label5.Visible = true;
                    }));
                    return;
                }

                HtmlAgilityPack.HtmlDocument data = await Schoology.LogIn(username, password, grade);

                if (data == null)
                {
                    Invoke(new Action(() =>
                    {

                        textBox1.Clear();
                        textBox2.Clear();

                        label5.Text = "Incorrect username or password.";
                        this.label5.Location = new System.Drawing.Point(278, 511);

                        button1.Enabled = true;

                        label5.Visible = true;

                        textBox1.Enabled = true;
                        textBox2.Enabled = true;

                        radioButton1.Enabled = true;
                        radioButton2.Enabled = true;
                        radioButton3.Enabled = true;
                        radioButton4.Enabled = true;
                        radioButton5.Enabled = true;
                    }));

                    return;
                }

                Invoke(new Action(() =>
                {
                    HomePage form = new HomePage(data, grade);
                    form.Show();
                    this.Hide();
                }));
            }).Start();
        }

        private void ForgotPasswordClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://inpsa.schoology.com/login/forgot");
        }

        private void TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void LoginPage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Schoology.chrome != null)
            {
                Schoology.chrome.Quit();
            }

            Environment.Exit(Environment.ExitCode);
        }
    }
}
