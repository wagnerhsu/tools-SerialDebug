using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using XMX.FileTransmit;
using System.Reflection;
using XMX.LIB;
using SunHealth.Gateway.Services.WxWindows;

namespace SerialDebug
{
    public partial class frmMain : Form
    {
        private ISendForm CurrentSendForm = null;
        private string Version = Application.ProductVersion;

        private Queue<SerialStreamContent> dataDispQueue = new Queue<SerialStreamContent>();
        private Thread dataStreamDisplayThread;
        private bool IsStart = false;

        private readonly Color ReceiveColor = Color.DarkRed;
        private readonly Color SendColor = Color.Blue;
        private CSerialDebug sp;
        private RadioButton[] rbtnSendMod;

        private enum SendModeType : int
        {
            Normal = 0,
            Queue,
            File,
        }

        private FormQueueSend frmQSend;
        private FormNormalSend frmNormalSend;
        private FormFileSend frmFileSend;
        private SendModeType sendModeType = SendModeType.Normal;
        private bool IsShowDataStreamInFileMode = false;

        private UInt64 RxCounter = 0;
        private UInt64 TxCounter = 0;
        private List<byte[]> reBytesList = new List<byte[]>();
        private Thread recThread;

        private List<string> SendTempList = new List<string>();
        private int SendTempIndex = 0;

        private delegate void TextBoxAppendDel(string str);             // �ı�������ַ�

        private TextBoxAppendDel txtReceiveAppend;

        private delegate void SetLableTextDel(Label lab, string Text);

        private SetLableTextDel SetLableText;

        private double splitPercent = 0.0f;

        private bool HyperTerminalMode = false;      // �����ն�ģʽ

        private void LoadConfig()
        {
            if (serialPort.IsOpen == false)
            {
                if (Properties.Settings.Default.comPort != string.Empty)
                {
                    cbComName.Text = Properties.Settings.Default.comPort;
                }
                else
                {
                    if (cbComName.Items.Count > 0)
                    {
                        cbComName.SelectedIndex = 0;
                    }
                }
            }

            cbBaudRate.Text = Properties.Settings.Default.comBaudRate.ToString();
            cbParity.SelectedIndex = Properties.Settings.Default.comParityBit;
            cbDataBit.SelectedIndex = Properties.Settings.Default.comDataBits;
            cbStopBit.SelectedIndex = Properties.Settings.Default.comStopBits;
            cbStreamControl.SelectedIndex = Properties.Settings.Default.comFlowCtrl;
            chkRTS.Checked = Properties.Settings.Default.comRTS;
            chkDTR.Checked = Properties.Settings.Default.comDTR;

            chkShowReceive.Checked = Properties.Settings.Default.dispReceive;
            chkShowSend.Checked = Properties.Settings.Default.dispSend;
            chkWrap.Checked = Properties.Settings.Default.dispWrap;
            chkReceiveHex.Checked = Properties.Settings.Default.dispHex;
            chkTimeStamp.Checked = Properties.Settings.Default.dispTimeStamp;
            numReceiveTimeOut.Value = Properties.Settings.Default.dispReceiveTimeOut;

            txtReceive.Font = Properties.Settings.Default.receiveFont;
        }

        private void SaveConfig()
        {
            Properties.Settings.Default.comPort = cbComName.Text;
            Properties.Settings.Default.comBaudRate = Convert.ToInt32(cbBaudRate.Text);
            Properties.Settings.Default.comParityBit = cbParity.SelectedIndex;
            Properties.Settings.Default.comDataBits = cbDataBit.SelectedIndex;
            Properties.Settings.Default.comStopBits = cbStopBit.SelectedIndex;
            Properties.Settings.Default.comFlowCtrl = cbStreamControl.SelectedIndex;
            Properties.Settings.Default.comRTS = chkRTS.Checked;
            Properties.Settings.Default.comDTR = chkDTR.Checked;

            Properties.Settings.Default.dispReceive = chkShowReceive.Checked;
            Properties.Settings.Default.dispSend = chkShowSend.Checked;
            Properties.Settings.Default.dispWrap = chkWrap.Checked;
            Properties.Settings.Default.dispHex = chkReceiveHex.Checked;
            Properties.Settings.Default.dispTimeStamp = chkTimeStamp.Checked;
            Properties.Settings.Default.dispReceiveTimeOut = (int)numReceiveTimeOut.Value;

            Properties.Settings.Default.Save();
        }

        private void SetMode(bool IsHyperTerminalMode)
        {
            groupReceive.Visible = !IsHyperTerminalMode;
            groupSend.Visible = !IsHyperTerminalMode;
            splitContainer1.Panel2Collapsed = IsHyperTerminalMode;
            groupHyperTerminal.Visible = IsHyperTerminalMode;

            HyperTerminalMode = IsHyperTerminalMode;

            if (IsHyperTerminalMode)
            {
                txtReceive.KeyPress += new KeyPressEventHandler(txtReceive_KeyPress);
            }
            else
            {
                txtReceive.KeyPress -= txtReceive_KeyPress;
            }
        }

        public frmMain()
        {
            InitializeComponent();

            this.Controls.Add(groupHyperTerminal);
            groupHyperTerminal.Location = groupReceive.Location;
            groupHyperTerminal.Visible = false;

            SetMode(false);

            ////���벨����
            //cbBaudRate.Items.Add(110);
            //cbBaudRate.Items.Add(300);
            //cbBaudRate.Items.Add(600);
            //cbBaudRate.Items.Add(1200);
            //cbBaudRate.Items.Add(2400);
            //cbBaudRate.Items.Add(4800);
            //cbBaudRate.Items.Add(9600);
            //cbBaudRate.Items.Add(14400);
            //cbBaudRate.Items.Add(19200);
            //cbBaudRate.Items.Add(28800);
            //cbBaudRate.Items.Add(38400);
            //cbBaudRate.Items.Add(56000);
            //cbBaudRate.Items.Add(57600);
            //cbBaudRate.Items.Add(128000);
            //cbBaudRate.Items.Add(115200);
            //cbBaudRate.Items.Add(256000);
            ////cbBaudRate.SelectedItem = 9600;
            //cbBaudRate.Text = Convert.ToString(9600);

            //��ż����λ
            cbParity.Items.Add(System.IO.Ports.Parity.Even);
            cbParity.Items.Add(System.IO.Ports.Parity.Mark);
            cbParity.Items.Add(System.IO.Ports.Parity.None);
            cbParity.Items.Add(System.IO.Ports.Parity.Odd);
            cbParity.Items.Add(System.IO.Ports.Parity.Space);
            cbParity.SelectedItem = System.IO.Ports.Parity.None;

            //����λ
            cbDataBit.Items.Add(5);
            cbDataBit.Items.Add(6);
            cbDataBit.Items.Add(7);
            cbDataBit.Items.Add(8);
            cbDataBit.SelectedItem = 8;

            //ֹͣλ
            //cbStopBit.Items.Add(System.IO.Ports.StopBits.None);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.One);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.OnePointFive);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.Two);
            cbStopBit.SelectedItem = System.IO.Ports.StopBits.One;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            sp = new CSerialDebug(serialPort);

            picPortState.Image = ImageList.Images["close"];
            picTop.Image = imglistTop.Images["nailoff"];
            cbComName.DataSource = SerialPort.GetPortNames();
            cbStreamControl.SelectedIndex = 0;
            serialPort.RtsEnable = chkRTS.Checked;

            Version = FileVersionService.GetFileVersion().FileVersion;
            //this.Text = string.Format("{0} V{1}    ���ߣ�����  QQ��516409354", Application.ProductName, Version);
            this.Text = string.Format("{0} V{1}", Application.ProductName, Version);

            CheckForIllegalCrossThreadCalls = false;

            txtReceiveAppend = new TextBoxAppendDel(TextBoxReceiveAppend);
            SetLableText = new SetLableTextDel(setLableText);
            cbHTEOFChars.SelectedIndex = 0;

            LoadConfig();

            panelNormalSend.Visible = false;

            frmNormalSend = new FormNormalSend();
            frmNormalSend.OnSendByCtrlEnter += new FormNormalSend.SendByCtrlEnterHandler(frmNormalSend_OnSendByCtrlEnter);
            frmNormalSend.Dock = DockStyle.Fill;
            frmNormalSend.FormBorderStyle = FormBorderStyle.None;
            frmNormalSend.TopLevel = false;
            frmNormalSend.Parent = splitContainer1.Panel2;
            frmNormalSend.Show();

            frmQSend = new FormQueueSend();
            frmQSend.ParamSetOpend += new EventHandler(frmQSend_ParamSetOpend);
            frmQSend.ParamSetClosed += new EventHandler(frmQSend_ParamSetClosed);
            frmQSend.ManualSendEvent += new FormQueueSend.ManualSendEventHandler(frmQSend_ManualSendEvent);
            frmQSend.Dock = DockStyle.Fill;
            frmQSend.FormBorderStyle = FormBorderStyle.None;
            frmQSend.TopLevel = false;
            frmQSend.Parent = splitContainer1.Panel2;
            frmQSend.Show();

            frmFileSend = new FormFileSend();
            frmFileSend.SendToUartEvent += new SendToUartEventHandler(frmFileSend_SendToUartEvent);
            frmFileSend.StartTransmitFile += new EventHandler(frmFileSend_StartTransmitFile);
            frmFileSend.EndTransmitFile += new EventHandler(frmFileSend_EndTransmitFile);
            frmFileSend.Dock = DockStyle.Fill;
            frmFileSend.FormBorderStyle = FormBorderStyle.None;
            frmFileSend.TopLevel = false;
            frmFileSend.Parent = splitContainer1.Panel2;
            frmFileSend.Show();

            radSendModeNormal.Checked = true;
            // setSendMode(SendModeType.Normal);

            int sendModeIndex = 0;
            sendModeIndex = Properties.Settings.Default.sendModeIndex;
            if (sendModeIndex >= 0 && sendModeIndex < 3)
            {
                setSendMode((SendModeType)sendModeIndex);
            }
            else
            {
                sendModeIndex = 0;
            }
            rbtnSendMod = new RadioButton[3] { radSendModeNormal, radSendModeQueue, radSendModeFile };
            rbtnSendMod[sendModeIndex].Checked = true;

            splitPercent = (double)splitContainer1.SplitterDistance / splitContainer1.Height;

            string encodingName = Properties.Settings.Default.Encoding;

            cbCharacterEncoding.DataSource = System.Text.Encoding.GetEncodings();
            cbCharacterEncoding.DisplayMember = "DisplayName";
            cbCharacterEncoding.ValueMember = "Name";

            if (encodingName == string.Empty)
            {
                encodingName = System.Text.Encoding.Default.BodyName;
            }
            SelectEncoding(encodingName);
        }

        private void SelectEncoding(string encodingName)
        {
            foreach (EncodingInfo e in cbCharacterEncoding.Items)
            {
                if (e.Name == encodingName)
                {
                    cbCharacterEncoding.SelectedItem = e;
                    Global.Encode = System.Text.Encoding.GetEncoding(encodingName);
                    if (encodingName != Properties.Settings.Default.Encoding)
                    {
                        Properties.Settings.Default.Encoding = encodingName;
                        Properties.Settings.Default.Save();
                    }

                    break;
                }
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveConfig();
                if (frmNormalSend != null)
                {
                    frmNormalSend.Close();
                }

                if (frmQSend != null)
                {
                    frmQSend.Close();
                }

                if (frmFileSend != null)
                {
                    frmFileSend.Close();
                }

                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                if (recThread != null)
                {
                    if (recThread.IsAlive)
                    {
                        recThread.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                recThread = null;
            }
        }

        #region ����������

        /// <summary>
        /// �򿪹رմ��ڲ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPortOpt_Click(object sender, EventArgs e)
        {
            ////throw new Exception("<xxxxxxxx&\"yyy\"\r\nzzz>");
            try
            {
                if (btnPortOpt.Text == "�򿪴���")
                {
                    IsStart = true;
                    dataDispQueue.Clear();
                    dataStreamDisplayThread = new Thread(new ThreadStart(dataDispThreadHandler));
                    dataStreamDisplayThread.IsBackground = true;
                    dataStreamDisplayThread.Start();

                    //serialPort.PortName = cbComName.SelectedItem.ToString();
                    Regex reg = new Regex(@"COM\d+");
                    Match matchs = reg.Match(cbComName.Text);
                    if (matchs != null && matchs.Groups[0].ToString() != string.Empty)
                    {
                        serialPort.PortName = matchs.Groups[0].ToString();
                    }
                    else
                    {
                        throw new Exception("�޷�ʶ��Ĵ��ڡ�");
                    }

                    serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                    serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                    serialPort.DataBits = (int)cbDataBit.SelectedItem;
                    serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;

                    serialPort.ReadBufferSize = 2 * 1024 * 1024;           // 2M
                    //serialPort.Open();
                    sp.ReceiveTimeOut = Convert.ToInt32(numReceiveTimeOut.Value);
                    sp.Start();
                    sp.ReceivedEvent += new CSerialDebug.ReceivedEventHandler(sp_ReceivedEvent);
                    sp.SendCompletedEvent += new CSerialDebug.SendCompletedEventHandler(sp_SendCompletedEvent);
                    sp.SendOverEvent += new EventHandler(sp_SendOverEvent);
                    txtReceive.ReadOnly = true;
                }
                else
                {
                    IsStart = false;

                    if (sendModeType != SendModeType.File)
                    {
                        sp.StopSend();
                    }
                    else
                    {
                        frmFileSend.Stop();
                    }

                    sp.ReceivedEvent -= new CSerialDebug.ReceivedEventHandler(sp_ReceivedEvent);
                    sp.SendCompletedEvent -= new CSerialDebug.SendCompletedEventHandler(sp_SendCompletedEvent);
                    sp.SendOverEvent -= new EventHandler(sp_SendOverEvent);

                    sp.Stop();
                    txtReceive.ReadOnly = false;
                    // serialPort.Close();
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    picPortState.Image = ImageList.Images["open"];
                    btnPortOpt.Text = "�رմ���";
                    cbComName.Enabled = false;
                    
                    UpdatalabText();
                }
                else
                {
                    picPortState.Image = ImageList.Images["close"];
                    btnPortOpt.Text = "�򿪴���";
                    cbComName.Enabled = true;
                    UpdatalabText();
                    SetSendEnable(false);
                }
            }
        }

        /// <summary>
        /// ѡ��ͨ�ſڡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbComName_SelectedIndexChanged(object sender, EventArgs e)
        {
            //string portname = serialPort.PortName;
            //try
            //{
            //    bool comOpend = serialPort.IsOpen;
            //    if (comOpend)
            //    {
            //        serialPort.Close();
            //    }
            //    serialPort.PortName = cbComName.SelectedItem.ToString();

            //    if (comOpend)
            //    {
            //        serialPort.Open();
            //    }
            //    UpdatalabText();
            //}
            //catch (Exception ex)
            //{
            //    cbComName.SelectedItem = portname;
            //    MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        /// <summary>
        /// �����ʡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int bps = serialPort.BaudRate;
            try
            {
                serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                cbBaudRate.Text = bps.ToString();
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// У��λ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbParity_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.Parity pt = serialPort.Parity;
            try
            {
                serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.Parity = pt;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����λ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            int db = serialPort.DataBits;
            try
            {
                serialPort.DataBits = Convert.ToInt32(cbDataBit.SelectedItem);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.DataBits = db;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ֹͣλ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.StopBits sb = serialPort.StopBits;
            try
            {
                serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.StopBits = sb;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbStreamControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                switch (cbStreamControl.SelectedIndex)
                {
                    case 0:
                        serialPort.Handshake = Handshake.None;
                        serialPort.RtsEnable = chkRTS.Checked;
                        serialPort.DtrEnable = chkDTR.Enabled;
                        break;

                    case 1:
                        serialPort.Handshake = Handshake.XOnXOff;
                        serialPort.RtsEnable = chkRTS.Checked;
                        serialPort.DtrEnable = chkDTR.Enabled;
                        break;

                    case 2:
                        serialPort.Handshake = Handshake.RequestToSend;
                        break;

                    case 3:
                        serialPort.Handshake = Handshake.RequestToSendXOnXOff;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ���ں�������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbComName_DropDown(object sender, EventArgs e)
        {
            string portName = cbComName.Text;

            //cbComName.DataSource = SerialPort.GetPortNames();
            //if (cbComName.Items.Contains(portName))
            //{
            //    cbComName.SelectedItem = portName;
            //}

            string[] portList = SystemHardware.GetSerialPort();

            int iMax = cbComName.Width;
            foreach (string s in portList)
            {
                //iMax = s.Length > iMax?s.Length:iMax;
                iMax = Math.Max(iMax, TextRenderer.MeasureText(s, cbComName.Font).Width);
            }
            cbComName.DropDownWidth = iMax;
            cbComName.DataSource = portList;
        }

        private void cbComName_DropDownClosed(object sender, EventArgs e)
        {
            //string portname = serialPort.PortName;
            //try
            //{
            //    bool comOpend = serialPort.IsOpen;
            //    if (comOpend)
            //    {
            //        serialPort.Close();
            //    }
            //    serialPort.PortName = cbComName.SelectedItem.ToString();

            //    if (comOpend)
            //    {
            //        serialPort.Open();
            //    }
            //    UpdatalabText();
            //}
            //catch (Exception ex)
            //{
            //    cbComName.SelectedItem = portname;
            //    MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        /// <summary>
        /// ����RTS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkRTS_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort.Handshake == Handshake.RequestToSend || serialPort.Handshake == Handshake.RequestToSendXOnXOff)
                {
                    chkRTS.Checked = !chkRTS.Checked;
                    MessageBox.Show("��������ѡ��Ӳ������Ӳ���������ʱ�޷���ȡ������DTS", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    serialPort.RtsEnable = chkRTS.Checked;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����DTR
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkDTR_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                serialPort.DtrEnable = chkDTR.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ˢ��״̬����
        /// </summary>
        private void UpdatalabText()
        {
            if (serialPort.IsOpen)
            {
                string str = string.Format("ͨ������({0},{1},{2},{3},{4})",
                      serialPort.PortName, serialPort.BaudRate, serialPort.Parity, (int)serialPort.DataBits,
                      (float)serialPort.StopBits);

                labIsSerialOpen.Text = str;
            }
            else
            {
                labIsSerialOpen.Text = "ͨ�ſ��ѹر�";
            }
        }

        #endregion ����������

        #region �Ҽ��˵�����

        private RichTextBox txtBoxMenu = new RichTextBox();

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuUndo_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Undo();
        }

        /// <summary>
        /// ���С�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCut_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Cut();
        }

        /// <summary>
        /// ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCopy_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Copy();
        }

        /// <summary>
        /// ճ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuPaste_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Paste();
        }

        /// <summary>
        /// ɾ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDelet_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectedText = "";
        }

        /// <summary>
        /// ȫѡ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSelectAll_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectAll();
        }

        /// <summary>
        /// �ַ���תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToHex_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.AsciiStringToArray(Global.Encode, txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToHexString(arr);

                Clipboard.SetText(str);
                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToString_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.HexStringToArray(txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToAsciiString(Global.Encode, arr);

                Clipboard.SetText(str);
                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ������תʮ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuBinaryToHex_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.BinaryStringToArray(txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToHexString(arr);
                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������ת������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToBinary_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.HexStringToArray(txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToBinaryString(arr);

                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToDec_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.HexStringToArray(txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToString(arr, 10);

                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToHex_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.DecimalStringToArray(txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToString(arr, 16);

                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �ַ���תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToDec_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.AsciiStringToArray(Global.Encode, txtBoxMenu.SelectedText);
                string str = StreamConverter.ArrayToString(arr, 10);

                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToString_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] arr = StreamConverter.DecimalStringToArray(txtBoxMenu.SelectedText);
                List<byte> l = new List<byte>();
                foreach (int v in arr)
                {
                    l.Add(Convert.ToByte(v & 0xFF));
                }
                string str = StreamConverter.ArrayToAsciiString(Global.Encode, l.ToArray());

                Clipboard.SetText(str);

                bool r = txtReceive.ReadOnly;
                txtReceive.ReadOnly = false;
                txtBoxMenu.Paste();
                txtReceive.ReadOnly = r;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �����Ҽ��˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmenuStrip_Opened(object sender, EventArgs e)
        {
            menuUndo.Enabled = txtBoxMenu.CanUndo;

            if (txtBoxMenu.SelectionLength > 0)
            {
                menuCut.Enabled = true;
                menuCopy.Enabled = true;
                menuDelet.Enabled = true;
                menuStringToHex.Enabled = true;
                menuHexToString.Enabled = true;
                menuHexToDec.Enabled = true;
                menuDecToHex.Enabled = true;
                menuStringToDec.Enabled = true;
                menuDecToString.Enabled = true;
            }
            else
            {
                menuCut.Enabled = false;
                menuCopy.Enabled = false;
                menuDelet.Enabled = false;
                menuStringToHex.Enabled = false;
                menuHexToString.Enabled = false;
                menuHexToDec.Enabled = false;
                menuDecToHex.Enabled = false;
                menuStringToDec.Enabled = false;
                menuDecToString.Enabled = false;
            }

            if (txtBoxMenu.Text == "")
            {
                menuSelectAll.Enabled = false;
            }
            else
            {
                menuSelectAll.Enabled = true;
            }
        }

        /// <summary>
        /// �������������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReceive_MouseEnter(object sender, EventArgs e)
        {
            txtBoxMenu = txtReceive;
        }

        /// <summary>
        /// �����뷢������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSend_MouseEnter(object sender, EventArgs e)
        {
            //txtBoxMenu = txtSend;
        }

        private void txtSend_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                txtSend.ContextMenuStrip = cmenuStrip;
            }
        }

        private void txtReceive_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                txtReceive.ContextMenuStrip = cmenuStrip;
            }
        }

        private void cmenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            txtBoxMenu.ContextMenuStrip = null;
        }

        #endregion �Ҽ��˵�����

        #region ���ܺ���

        /// <summary>
        /// ʮ�������ַ���תʮ���ơ�
        /// </summary>
        /// <param name="hexStr"></param>
        /// <returns></returns>
        private byte HexStringToByte(string hexStr)
        {
            return Convert.ToByte(hexStr, 16);
        }

        /// <summary>
        /// ʮ�����ַ���תʮ���ơ�
        /// </summary>
        /// <param name="decStr"></param>
        /// <returns></returns>
        private byte DecStringToByte(string decStr)
        {
            return Convert.ToByte(decStr, 10);
        }

        #endregion ���ܺ���

        #region ״̬������

        /// <summary>
        /// ���ý���������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picReceiveFont_Click(object sender, EventArgs e)
        {
            fontDlg.Font = txtReceive.Font;
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                txtReceive.Font = fontDlg.Font;
                //txtSend.Font = fontDlg.Font;
                Properties.Settings.Default.receiveFont = fontDlg.Font;
                Properties.Settings.Default.Save();
            }
        }

        private void picReloadConfig_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reset();
            LoadConfig();

            if (frmNormalSend != null)
            {
                frmNormalSend.LoadConfig();
            }

            if (frmQSend != null)
            {
                frmQSend.LoadConfig();
            }

            if (frmFileSend != null)
            {
                frmFileSend.LoadConfig();
            }
        }

        /// <summary>
        /// �ö�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void picTop_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            if (this.TopMost == true)
            {
                this.Text = Application.ProductName + " V" + Version + "  [�ö�]";
                picTop.Image = imglistTop.Images["nailon"];
            }
            else
            {
                this.Text = Application.ProductName + " V" + Version;
                picTop.Image = imglistTop.Images["nailoff"];
            }

            string textToolTip;
            if (this.TopMost)
            {
                textToolTip = "ȡ���ö�";
            }
            else
            {
                textToolTip = "�ö�";
            }
            ToolTip.SetToolTip(picTop, textToolTip);
        }

        /// <summary>
        /// ��ս�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labClearReceive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtReceive.Clear();
        }

        /// <summary>
        /// ��շ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labClearSend_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSend.Clear();
        }

        /// <summary>
        /// ��ռ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
            labTx.Text = "TX:0";
            TxCounter = 0;
        }

        /// <summary>
        /// ��ս��ռ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labRx_DoubleClick(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
        }

        /// <summary>
        /// ��շ��ͼ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labTx_DoubleClick(object sender, EventArgs e)
        {
            labTx.Text = "TX:0";
            TxCounter = 0;
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHelp_Click(object sender, EventArgs e)
        {
            bool topmost = this.TopMost;

            this.TopMost = false;
            AboutBox myAboutBox = new AboutBox();
            myAboutBox.ShowDialog();
            this.TopMost = topmost;
        }

        /// <summary>
        /// �ر�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEnd_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion ״̬������

        #region ���ڽ�����ʾ

        ///// <summary>
        ///// ���ڽ����жϡ�
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    try
        //    {
        //        int bytesLen = 0;
        //        byte[] bytes;
        //        do
        //        {
        //            bytesLen = serialPort.BytesToRead;
        //            if (bytesLen >= 4096)
        //            {
        //                bytes = new byte[bytesLen];
        //                if (bytesLen <= 0)
        //                {
        //                    return;
        //                }
        //                serialPort.Read(bytes, 0, bytesLen);
        //                lock (reBytesList)
        //                {
        //                    reBytesList.Add(bytes);
        //                }
        //            }
        //            else
        //            {
        //                Thread.Sleep(30);
        //            }
        //        } while (bytesLen != serialPort.BytesToRead);

        //        if (bytesLen <= 0)
        //        {
        //            return;
        //        }
        //        bytes = new byte[bytesLen];
        //        serialPort.Read(bytes, 0, bytesLen);
        //        lock (reBytesList)
        //        {
        //            reBytesList.Add(bytes);
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("���ڽ���" + ex.Message);
        //    }
        //}

        ///// <summary>
        ///// ���մ����̡߳�
        ///// </summary>
        //private void ReceiveThreadHandle()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            SerialDebugReceiveData data = null;
        //            lock (sp.ReceiveQueue)
        //            {
        //                if (sp.ReceiveQueue.Count > 0)
        //                {
        //                    data = sp.ReceiveQueue.Dequeue();
        //                }
        //            }

        //            if (data != null)
        //            {
        //                StringBuilder sbMsg = new StringBuilder();

        //                if (chkDisplay.Checked)  // �Ƿ���ʾ
        //                {
        //                    if (chkTimeStamp.Checked)
        //                    {
        //                        sbMsg.AppendFormat("<<<{0}", data.TimeString);
        //                    }

        //                    if (chkReceiveHex.Checked) // ʮ��������ʾ
        //                    {
        //                        sbMsg.AppendFormat("{0}", data.HexString);

        //                    }
        //                    else
        //                    {
        //                        sbMsg.AppendFormat("{0}", data.ASCIIString);
        //                    }

        //                    if (chkWrap.Checked || chkTimeStamp.Checked)                    // �Զ�����
        //                    {
        //                        sbMsg.Append(Environment.NewLine);
        //                    }
        //                }
        //                TextBoxReceiveAppend(sbMsg.ToString());
        //                RxCounter = RxCounter + (UInt64)data.DataLen;
        //                setLableText(labRx, string.Format("RX:{0}", RxCounter));
        //            }
        //            else
        //            {
        //                Thread.Sleep(100);
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("���ݴ����̣߳�" + ex.Message);
        //        }
        //    }
        //}

        /// <summary>
        /// ���½��ճ��ȡ�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateRx(UInt64 count)
        {
            labRx.Text = "RX:" + count.ToString();
            labRx.Refresh();
        }

        private void setLableText(Label lab, string text)
        {
            if (lab.InvokeRequired)
            {
                lab.BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate
                {
                    SetLableText(lab, text);
                }));
            }
            else
            {
                lab.Text = text;
            }
        }

        private void TextBoxReceiveAppend(string appendText)
        {
            TextBoxReceiveAppend(Color.Black, appendText);
        }

        /// <summary>
        /// ���½����ı���
        /// </summary>
        /// <param name="appendText"></param>
        private void TextBoxReceiveAppend(Color color, string appendText)
        {
            if (txtReceive.InvokeRequired)
            {
                txtReceive.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                {
                    TextBoxReceiveAppend(color, appendText);
                }));
            }
            else
            {
                if (appendText == string.Empty)
                {
                    return;
                }

                if (HyperTerminalMode == true)
                {
                    HyperTerminalShowText(appendText);
                }
                else
                {
                    txtReceive.SelectionStart = txtReceive.Text.Length;
                    //txtReceive.SelectionLength = 0;

                    txtReceive.SelectionColor = color;
                    txtReceive.AppendText(appendText);
                    txtReceive.ScrollToCaret();
                }
            }
        }

        /// <summary>
        /// ���ý��ճ�ʱʱ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numReceiveTimeOut_ValueChanged(object sender, EventArgs e)
        {
            if (sp != null)
            {
                sp.ReceiveTimeOut = Convert.ToInt32(numReceiveTimeOut.Value);
            }
        }

        private void DisplayContent(SerialStreamType type, string text)
        {
            //�ϴ�Ϊ��������
            if (text != "")
            {
                switch (type)
                {
                    case SerialStreamType.Receive:
                        TextBoxReceiveAppend(ReceiveColor, text.ToString());
                        setLableText(labRx, string.Format("RX:{0}", RxCounter));
                        break;

                    case SerialStreamType.Send:
                        TextBoxReceiveAppend(SendColor, text.ToString());
                        setLableText(labTx, string.Format("TX:{0}", TxCounter));
                        break;
                }
            }
        }

        private void dataDispThreadHandler()
        {
            UInt64 rxInc = RxCounter;
            UInt64 txInc = TxCounter;
            DateTime lastUpdateTime = DateTime.Now;
            SerialStreamType lastUpdateType = SerialStreamType.Receive;
            StringBuilder rxStrBuff = new StringBuilder();
            StringBuilder txStrBuff = new StringBuilder();

            while (IsStart)
            {
                SerialStreamContent content = null;

                lock (dataDispQueue)
                {
                    if (dataDispQueue.Count > 0)
                    {
                        content = dataDispQueue.Dequeue();
                    }
                }
                if (content != null)
                {
                    switch (content.Type)
                    {
                        case SerialStreamType.Receive:

                            if (lastUpdateType != SerialStreamType.Receive)
                            {
                                DisplayContent(lastUpdateType, txStrBuff.ToString());
                                txStrBuff.Remove(0, txStrBuff.Length);
                            }

                            RxCounter += (UInt64)content.DataLen;
                            lastUpdateType = content.Type;
                            rxStrBuff.Append(content.Content);

                            break;

                        case SerialStreamType.Send:

                            if (lastUpdateType != SerialStreamType.Send)
                            {
                                DisplayContent(lastUpdateType, rxStrBuff.ToString());
                                rxStrBuff.Remove(0, rxStrBuff.Length);
                            }

                            TxCounter += (UInt64)content.DataLen;
                            lastUpdateType = content.Type;
                            txStrBuff.Append(content.Content);

                            break;

                        default:
                            break;
                    }
                    lastUpdateType = content.Type;
                }
                else
                {
                    if (rxStrBuff.Length > 0)
                    {
                        DisplayContent(lastUpdateType, rxStrBuff.ToString());
                        rxStrBuff.Remove(0, rxStrBuff.Length);
                    }

                    if (txStrBuff.Length > 0)
                    {
                        DisplayContent(lastUpdateType, txStrBuff.ToString());
                        txStrBuff.Remove(0, txStrBuff.Length);
                    }
                    Thread.Sleep(10);
                }

                //TimeSpan ts = DateTime.Now - lastUpdateTime;
                //if (ts.TotalMilliseconds >= 1000)
                //{
                //    if (rxInc != RxCounter)
                //    {
                //        TextBoxReceiveAppend(ReceiveColor, rxStrBuff.ToString());
                //        rxStrBuff = new StringBuilder();

                //        setLableText(labRx, string.Format("RX:{0}", RxCounter));
                //        rxInc = RxCounter;
                //    }

                //    if (txInc != TxCounter)
                //    {
                //        TextBoxReceiveAppend(SendColor, txStrBuff.ToString());
                //        txStrBuff = new StringBuilder();

                //        setLableText(labTx, string.Format("TX:{0}", TxCounter));
                //        txInc = TxCounter;
                //    }
                //    lastUpdateTime = DateTime.Now;
                //}
                //else
                //{
                //    if (rxInc != RxCounter)
                //    {
                //        TextBoxReceiveAppend(ReceiveColor, rxStrBuff.ToString());
                //        rxStrBuff = new StringBuilder();

                //        setLableText(labRx, string.Format("RX:{0}", RxCounter));
                //        rxInc = RxCounter;
                //    }

                //    if (txInc != TxCounter)
                //    {
                //        TextBoxReceiveAppend(SendColor, txStrBuff.ToString());
                //        txStrBuff = new StringBuilder();

                //        setLableText(labTx, string.Format("TX:{0}", TxCounter));
                //        txInc = TxCounter;
                //    }

                //    Thread.Sleep(100);
                //}
            }

            lock (dataDispQueue)
            {
                dataDispQueue.Clear();
            }
        }

        #endregion ���ڽ�����ʾ

        #region ���ڷ���

        /// <summary>
        /// ����״̬�����ա�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateTx(UInt64 count)
        {
            labTx.Text = "TX:" + count.ToString();
            labTx.Refresh();
        }

        /// <summary>
        /// ���·������ı�
        /// </summary>
        /// <param name="text"></param>
        private void txtSendUpdate(string text)
        {
            if (txtSend.InvokeRequired)
            {
                txtSend.BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate
                {
                    txtSendUpdate(text);
                }));
            }
            else
            {
                txtSend.Text = text;
            }
        }

        /// <summary>
        /// ���°������������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSend_KeyDown(object sender, KeyEventArgs e)
        {
            string text = "";
            if (e.Modifiers == Keys.Control)
            {
                IsCtrlPressed = true;
            }
            else
            {
                IsCtrlPressed = false;
            }

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter)
            {
                btnSend.PerformClick();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Up)
            {
                SendTempIndex--;
                if (SendTempIndex < 0)
                {
                    SendTempIndex = 0;
                    Console.Beep();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                SendTempIndex++;
                if (SendTempIndex > SendTempList.Count)
                {
                    SendTempIndex = SendTempList.Count;
                    Console.Beep();
                }
                e.Handled = true;
            }
            else
            {
                return;
            }

            lock (SendTempList)
            {
                if (SendTempList.Count > 0)
                {
                    if (SendTempIndex < SendTempList.Count)
                    {
                        text = SendTempList[SendTempIndex];
                    }
                }
            }

            txtSend.Clear();
            txtSend.Text = text;
            //txtSend.AppendText(text);
        }

        private bool IsCtrlPressed = false;

        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (IsCtrlPressed)
            {
                if (e.KeyChar == '\r' || e.KeyChar == '\n') // �س�
                {
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// �����ʼ���ͻ���ֹͣ���͡�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnSend.Text == "��ʼ����")
                {
                    if (serialPort.IsOpen == false)
                    {
                        MessageBox.Show("����δ�򿪣����ȴ򿪴���", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }

                    CurrentSendForm = (ISendForm)frmNormalSend;
                    switch (sendModeType)
                    {
                        case SendModeType.Normal:
                            CurrentSendForm = (ISendForm)frmNormalSend;
                            break;

                        case SendModeType.Queue:
                            CurrentSendForm = (ISendForm)frmQSend;
                            break;

                        case SendModeType.File:
                            CurrentSendForm = (ISendForm)frmFileSend;
                            IsShowDataStreamInFileMode = frmFileSend.ShowDataStream;
                            frmFileSend.Start();
                            break;
                    }

                    if (sendModeType != SendModeType.File)
                    {
                        List<CSendParam> list = CurrentSendForm.GetSendList();
                        if (list.Count <= 0)
                        {
                            MessageBox.Show("û���κοɷ��͵�����", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        else
                        {
                            SetSendEnable(true);
                            sp.Send(list, CurrentSendForm.LoopCount);
                        }
                    }
                    else
                    {
                        SetSendEnable(true);
                    }

                    //SetSendEnable(true);
                    //setLableText(labTx, string.Format("TX:{0}", TxCounter));
                }
                else
                {
                    if (sendModeType != SendModeType.File)
                    {
                        sp.StopSend();
                    }
                    else
                    {
                        frmFileSend.Stop();
                    }

                    //SerialSendAbort = true;
                    SetSendEnable(false);
                    setLableText(labTx, string.Format("TX:{0}", TxCounter));
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ���÷���ʹ�ܡ�
        /// </summary>
        /// <param name="IsEnable">��ΪTrueʱ��ʾ��ʼ���ͣ�False��ʾֹͣ���͡�</param>
        private void SetSendEnable(bool IsEnable)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new System.Windows.Forms.MethodInvoker(delegate ()
                {
                    SetSendEnable(IsEnable);
                }));
                //return;
            }
            else
            {
                if (IsEnable == true)
                {
                    if (btnSend.Text != "ֹͣ����")
                    {
                        btnSend.Text = "ֹͣ����";

                        radSendModeNormal.Enabled = false;
                        radSendModeQueue.Enabled = false;
                        radSendModeFile.Enabled = false;
                        if (CurrentSendForm != null)
                        {
                            CurrentSendForm.EditEnable = false;
                        }
                    }
                }
                else
                {
                    if (btnSend.Text != "��ʼ����")
                    {
                        btnSend.Text = "��ʼ����";

                        radSendModeNormal.Enabled = true;
                        radSendModeQueue.Enabled = true;
                        radSendModeFile.Enabled = true;
                        if (CurrentSendForm != null)
                        {
                            CurrentSendForm.EditEnable = true;
                        }
                    }
                }
            }
        }

        #endregion ���ڷ���

        #region �����ļ��ʹ��ļ�

        /// <summary>
        /// ��������˵�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lnkSaveData_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (txtReceive.Text == "")
            {
                cmenuSave.Enabled = false;
            }
            else
            {
                cmenuSave.Enabled = true;
            }
            this.cmenuSave.Show(lnkSaveData, 0, lnkSaveData.Height);
        }

        /// <summary>
        /// ��������˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lnkSaveData_Enter(object sender, EventArgs e)
        {
            //if (txtReceive.Text == "")
            //{
            //    cmenuSave.Enabled = false;
            //}
            //else
            //{
            //    cmenuSave.Enabled = true;
            //}
            //this.cmenuSave.Show(lnkSaveData, 0, lnkSaveData.Height);
        }

        /// <summary>
        /// ��ԭʼ��ʾ���档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToText_Click(object sender, EventArgs e)
        {
            sFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sFileDlg.FileName, txtReceive.Text);
                MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// ��������תΪ��������ʾ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToBinary_Click(object sender, EventArgs e)
        {
            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {
                    byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtReceive.Text);
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// ��ʮ�����Ƶ��������ļ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveHexToBinary_Click(object sender, EventArgs e)
        {
            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {
                    //string[] strArray = txtReceive.Text.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] strArray = txtReceive.Text.TrimEnd().Replace(Environment.NewLine, "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }
        }

        private void lnkOpen_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            oFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt|�������ļ�(*.bin)|*.bin|�����ļ�(*.*)|*.*";
            if (oFileDlg.ShowDialog() == DialogResult.OK)
            {
                string strExt = System.IO.Path.GetExtension(oFileDlg.FileName).ToUpper();
                if (strExt == ".TXT")
                {
                    txtSend.Text = File.ReadAllText(oFileDlg.FileName);
                }
                else
                {
                    FileStream fs = new FileStream(oFileDlg.FileName, FileMode.Open);
                    BinaryReader br = new BinaryReader(fs);
                    try
                    {
                        byte[] bytes = br.ReadBytes((int)fs.Length);
                        txtSend.Text = BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, oFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        br.Close();
                        fs.Close();
                    }
                }
            }
        }

        #endregion �����ļ��ʹ��ļ�

        #region �����ն�ģʽ

        /// <summary>
        /// �����ն���ʾ�ı�
        /// </summary>
        /// <param name="appendText"></param>
        private void HyperTerminalShowText(string appendText)
        {
            HyperTerminal_HandleMessage(appendText);
            return;
#if OLD_SHOW_HYPER

            #region �����ն�ģʽ��ʾ

            string[] textBoxArray = txtReceive.Lines;
            int indexLines = txtReceive.Lines.Length;
            if (indexLines > 0)
            {
                indexLines = indexLines - 1;
            }

            int strIndex = -1;

            string[] textLines = appendText.Split(new string[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.None);

            int index = 0;
            foreach (string strLine in textLines)
            {
                string str = strLine;
                textBoxArray = txtReceive.Lines;

                do
                {
                    strIndex = str.IndexOf('\b');
                    if (strIndex == 0)
                    {
                        string tempStr = string.Empty;

                        str = str.Remove(0, 1);
                        if (txtReceive.Text.Length > 0)
                        {
                            txtReceive.Text = txtReceive.Text.Remove(txtReceive.Text.Length - 1, 1);
                            textBoxArray = txtReceive.Lines;
                            indexLines = txtReceive.Lines.Length;
                            if (indexLines > 0)
                            {
                                indexLines = indexLines - 1;
                            }
                        }
                    }
                    else if (strIndex > 0)
                    {
                        str = str.Remove(strIndex - 1, 2);
                    }
                } while (strIndex >= 0);

                strIndex = str.IndexOf('\r');
                if (strIndex >= 0)
                {
                    string[] rArray = str.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    string rText = string.Empty;
                    foreach (string rStr in rArray)
                    {
                        if (rStr.Length > rText.Length)
                        {
                            rText = rStr;
                        }
                        else
                        {
                            rText.Replace(rText.Substring(0, rStr.Length), rStr);
                        }
                    }
                    if (rText.Length != 0)
                    {
                        textBoxArray[indexLines] = rText;
                        txtReceive.Lines = textBoxArray;
                        if (str.EndsWith("\r") == false)
                        {
                            txtReceive.AppendText(Environment.NewLine);
                            indexLines++;
                        }
                    }
                    else
                    {
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexFromLine(indexLines);
                    }
                }
                else
                {
                    txtReceive.AppendText(str);
                    if (index < textLines.Length - 1)
                    {
                        txtReceive.AppendText(Environment.NewLine);
                        indexLines++;
                    }
                }
                index++;
            }

            #endregion �����ն�ģʽ��ʾ

#endif
        }

        /// <summary>
        /// V3.1����
        /// </summary>
        /// <param name="message"></param>
        private void HyperTerminal_HandleMessage(string message)
        {
            string[] txtArray = txtReceive.Lines;
            string[] appendLines = message.Split(new string[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.None);

            int appendLineIndex = 0;
            int rowIndex = 0;
            int searchCharIndex = -1;
            string outStr;
            string inStr;
            foreach (string appendStr in appendLines)
            {
                inStr = appendStr;
                rowIndex = 0;
                do
                {
                    if (rowIndex >= appendStr.Length) break;

                    searchCharIndex = appendStr.IndexOf('\b', rowIndex);
                    if (searchCharIndex > 0)
                    {
                        inStr = inStr.Remove(searchCharIndex - 1, 2);
                    }
                    else if (searchCharIndex == 0)
                    {
                        inStr = inStr.Remove(0, 1);
                        if (txtReceive.SelectionStart > 0)
                        {
                            txtReceive.SelectionStart--;
                            txtReceive.SelectionLength = 1;
                            txtReceive.SelectedText = "";
                        }
                    }
                    rowIndex += searchCharIndex + 1;
                } while (searchCharIndex >= 0);

                outStr = inStr;
                rowIndex = 0;
                do
                {
                    if (rowIndex >= inStr.Length)
                    {
                        outStr = "";
                        break;
                    }
                    else
                    {
                        outStr = inStr.Substring(rowIndex, inStr.Length - rowIndex);
                    }

                    searchCharIndex = inStr.IndexOf('\r', rowIndex);
                    if (searchCharIndex < 0)
                    {
                        break;
                    }

                    outStr = inStr.Substring(rowIndex + 1);
                    if (searchCharIndex == 0)
                    {
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexOfCurrentLine();
                    }
                    else if (searchCharIndex > 0)
                    {
                        txtReceive.SelectionLength = searchCharIndex - rowIndex;
                        txtReceive.SelectedText = inStr.Substring(rowIndex, searchCharIndex - rowIndex);
                        txtReceive.SelectionStart = txtReceive.GetFirstCharIndexOfCurrentLine();
                    }
                    rowIndex += searchCharIndex + 1;
                } while (searchCharIndex >= 0);

                appendLineIndex++;
                txtReceive.SelectionLength = outStr.Length;
                txtReceive.SelectedText = outStr;
                if (appendLineIndex < appendLines.Length)
                {
                    txtReceive.SelectionStart = txtReceive.Text.Length;
                    txtReceive.SelectedText = Environment.NewLine;
                }
            }
        }

        private string htSendString = string.Empty;

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReceive_KeyPress(object sender, KeyPressEventArgs e)
        {
            //txtSend.Text = string.Format("KeyChar:{0}", (int)e.KeyChar);
            if (HyperTerminalMode == false || serialPort.IsOpen == false)
            {
                return;
            }

            if (chkSendByEnter.Checked)
            {
                if (e.KeyChar == 8)     // �˸�
                {
                    if (htSendString.Length > 0)
                    {
                        htSendString = htSendString.Remove(htSendString.Length - 1, 1);
                    }
                }
                if (e.KeyChar == 13)    // �س�
                {
                    serialPort.Write(string.Format("{0}{1}", htSendString, HtEofChars));
                    htSendString = string.Empty;
                }
                else
                {
                    htSendString += (char)e.KeyChar;
                }
            }
            else
            {
                serialPort.Write(new byte[] { (byte)e.KeyChar }, 0, 1);
            }
            if (chkHTCharEcho.Checked)
            {
                //e.Handled = true;
                txtReceive.AppendText(Char.ToString(e.KeyChar));
            }
        }

        /// <summary>
        /// ��ͨģʽ�������ն�ģʽ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnNormalToHyperTerminal_Click(object sender, EventArgs e)
        {
            SetMode(true);
        }

        /// <summary>
        /// �����ն�ģʽ����ͨģʽ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHyperTerminalToNormal_Click(object sender, EventArgs e)
        {
            SetMode(false);
        }

        private string HtEofChars = string.Empty;       // �س�����ʱ������ֹ��

        /// <summary>
        /// ѡ�������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbHTEOFChars_SelectedIndexChanged(object sender, EventArgs e)
        {
            //NONE
            //NULL��\0��
            //LF��\n��
            //CR+LF��\r\n��
            //LF+CR��\n\r��
            //CR��\r��

            switch (cbHTEOFChars.SelectedIndex)
            {
                case 0:
                    HtEofChars = string.Empty;
                    break;

                case 1:
                    HtEofChars = "\0";
                    break;

                case 2:
                    HtEofChars = "\n";
                    break;

                case 3:
                    HtEofChars = "\r\n";
                    break;

                case 4:
                    HtEofChars = "\n\r";
                    break;

                case 5:
                    HtEofChars = "\r";
                    break;
            }
        }

        #endregion �����ն�ģʽ

        /// <summary>
        /// �����¼�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sp_ReceivedEvent(object sender, SerialDebugReceiveData e)
        {
            if (e != null)
            {
                string msg = "";
                StringBuilder sbMsg = new StringBuilder();

                if (chkShowReceive.Checked)  // �Ƿ���ʾ
                {
                    if (chkTimeStamp.Checked)
                    {
                        sbMsg.AppendFormat("{0}[<--]", e.TimeString);
                    }

                    if (chkReceiveHex.Checked) // ʮ��������ʾ
                    {
                        sbMsg.AppendFormat("{0}", e.HexString);
                    }
                    else
                    {
                        sbMsg.AppendFormat("{0}", e.ASCIIString);
                    }

                    if (chkWrap.Checked || chkTimeStamp.Checked)                    // �Զ�����
                    {
                        sbMsg.Append(Environment.NewLine);
                    }

                    if (sendModeType != SendModeType.File || (sendModeType == SendModeType.File && IsShowDataStreamInFileMode))
                    {
                        //TextBoxReceiveAppend(ReceiveColor, sbMsg.ToString());
                        msg = sbMsg.ToString();
                    }
                }
                lock (dataDispQueue)
                {
                    dataDispQueue.Enqueue(new SerialStreamContent(SerialStreamType.Receive, sbMsg.ToString(), e.DataLen));
                }

                // RxCounter = RxCounter + (UInt64)e.DataLen;
                // setLableText(labRx, string.Format("RX:{0}", RxCounter));
            }

            if (sendModeType == SendModeType.File)
            {
                frmFileSend.ReceivedFromUart(e.ReceiveData);
            }
        }

        /// <summary>
        /// ������ʾ�¼�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sp_SendCompletedEvent(object sender, SendCompletedEventArgs e)
        {
            if (e.SendParam == null)
            {
                if (sendModeType != SendModeType.File)
                {
                    SetSendEnable(false);
                }
            }
            else
            {
                string msg = "";
                StringBuilder sendMsg = new StringBuilder();

                if (chkShowSend.Checked)
                {
                    if (chkTimeStamp.Checked)
                    {
                        sendMsg.AppendFormat("{0}[-->]", e.TimeString);
                    }

                    if (chkReceiveHex.Checked)
                    {
                        sendMsg.AppendFormat("{0}", e.SendParam.HexString);
                    }
                    else
                    {
                        sendMsg.AppendFormat("{0}", e.SendParam.ASCIIString);
                    }

                    if (chkTimeStamp.Checked || chkWrap.Checked)
                    {
                        sendMsg.Append(Environment.NewLine);
                    }

                    if (sendModeType != SendModeType.File || (sendModeType == SendModeType.File && IsShowDataStreamInFileMode))
                    {
                        //TextBoxReceiveAppend(SendColor, sendMsg.ToString());
                        msg = sendMsg.ToString();
                    }
                }

                lock (dataDispQueue)
                {
                    dataDispQueue.Enqueue(new SerialStreamContent(SerialStreamType.Send, sendMsg.ToString(), e.SendParam.DataLen));
                }

                //TxCounter = TxCounter + (UInt64)(e.SendParam.DataLen);
                //setLableText(labTx, string.Format("TX:{0}", TxCounter));
            }
        }

        private void sp_SendOverEvent(object sender, EventArgs e)
        {
            if (sendModeType != SendModeType.File)
            {
                SetSendEnable(false);
            }
        }

        private void frmFileSend_EndTransmitFile(object sender, EventArgs e)
        {
            SetSendEnable(false);
        }

        private void frmFileSend_StartTransmitFile(object sender, EventArgs e)
        {
            SetSendEnable(true);
        }

        private void frmFileSend_SendToUartEvent(object sender, SendToUartEventArgs e)
        {
            List<CSendParam> list = new List<CSendParam>();
            list.Add(new CSendParam(SendParamFormat.Hex, SendParamMode.SendAfterLastSend, 0, e.Data, 0, e.Data.Length));
            sp.Send(list);
        }

        private void frmNormalSend_OnSendByCtrlEnter(object sender, EventArgs e)
        {
            btnSend.PerformClick();
        }

        private void frmQSend_ManualSendEvent(object sender, ManualSendEventArgs e)
        {
            if (e != null)
            {
                sp.Send(e.SendList);
            }
        }

        private void frmQSend_ParamSetClosed(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance += frmQSend.ParamSetHeight;

            // splitContainer1.SplitterDistance = Convert.ToInt32(splitPercent * splitContainer1.Height);
        }

        private void frmQSend_ParamSetOpend(object sender, EventArgs e)
        {
            splitContainer1.SplitterDistance -= frmQSend.ParamSetHeight;
        }

        private void radSendMode_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radSendMode = sender as RadioButton;
            if (radSendMode == null)
            {
                return;
            }

            if (radSendMode.Name == radSendModeNormal.Name)
            {
                setSendMode(SendModeType.Normal);
            }
            else if (radSendMode.Name == radSendModeQueue.Name)
            {
                setSendMode(SendModeType.Queue);
            }
            else if (radSendMode.Name == radSendModeFile.Name)
            {
                setSendMode(SendModeType.File);
            }
        }

        private void setSendMode(SendModeType type)
        {
            frmQSend.Hide();
            frmFileSend.Hide();
            frmNormalSend.Hide();
            sendModeType = type;
            switch (type)
            {
                case SendModeType.Normal:
                    frmNormalSend.Show();
                    break;

                case SendModeType.Queue:
                    frmQSend.Show();
                    break;

                case SendModeType.File:
                    frmFileSend.Show();
                    break;
            }

            Properties.Settings.Default.sendModeIndex = (int)type;
            Properties.Settings.Default.Save();
        }

        private void cbCharacterEncoding_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbCharacterEncoding.SelectedItem != null)
            {
                string EncodingName = (cbCharacterEncoding.SelectedItem as EncodingInfo).Name;
                SelectEncoding(EncodingName);
            }
        }

        private ToolTip comboToolTip = new ToolTip();

        private void cbComName_MouseHover(object sender, EventArgs e)
        {
            comboToolTip.SetToolTip(cbComName, cbComName.Text);
        }
    }

    public enum SerialStreamType : int
    {
        Receive,
        Send,
    }

    public class SerialStreamContent
    {
        private readonly string _Content;
        private readonly SerialStreamType _StreamType;
        private readonly int _DataLen;

        public SerialStreamContent(SerialStreamType type, string content, int dataLen)
        {
            _Content = content;
            _DataLen = dataLen;
            _StreamType = type;
        }

        public SerialStreamType Type
        {
            get { return _StreamType; }
        }

        public string Content
        {
            get { return _Content; }
        }

        public int DataLen
        {
            get { return _DataLen; }
        }
    }
}