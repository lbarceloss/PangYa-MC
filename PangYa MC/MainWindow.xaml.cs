using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;


namespace PangYa_MC
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EnableBlur();
        }

        private void btnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy { AccentState = 3 };
            var accentSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = 19,
                Data = accentPtr,
                SizeOfData = accentSize
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int Flags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private void btnSelecionar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Executável (*.exe)|*.exe"
            };
            if (ofd.ShowDialog() == true)
            {
                txtPath.Text = ofd.FileName;
            }
        }

        private async void btnIniciar_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();

            string exePath = txtPath.Text.Trim();
            if (!File.Exists(exePath))
            {
                Log("[ERRO] Caminho inválido ou arquivo não encontrado.", LogType.Error);
                return;
            }

            try
            {
                // Verifica se ProjectG.exe já está rodando
                Process[] procs = Process.GetProcessesByName("ProjectG");
                if (procs.Length > 0)
                {
                    // Se estiver em execução, faz o fechamento do Mutex
                    Log("[INFO] ProjectG.exe já está em execução. Fechando Mutex...", LogType.Info);

                    bool result = false;
                    foreach (var p in procs)
                    {
                        result |= TryKillMutexInSessions(p.Id);
                    }

                    if (result)
                    {
                        Log("[SUCESSO] Mutex fechado! ", LogType.Success);
                        await Task.Delay(7000);
                        Log("[INFO] Iniciando nova instância do jogo...", LogType.Info);
                        Process.Start(exePath);
                    }
                    else
                    {
                        Log("[ERRO] Não foi possível fechar o Mutex (Sessions 1, 2 ou 3).", LogType.Error);
                        Log("[INFO] Mesmo assim, iniciando o jogo...", LogType.Info);
                        Process.Start(exePath);
                    }
                }
                else
                {
                    // Se NÃO estiver em execução, inicia o jogo imediatamente
                    Log("[INFO] ProjectG.exe não está rodando. Iniciando o jogo agora...", LogType.Info);
                    Process.Start(exePath);
                }
            }
            catch (Exception ex)
            {
                Log($"[EXCEÇÃO] {ex.Message}", LogType.Error);
            }
        }
        private enum LogType { Info, Success, Error }

        private void Log(string message, LogType type = LogType.Info)
        {
            Brush color = Brushes.White;
            switch (type)
            {
                case LogType.Info: color = Brushes.Yellow; break;
                case LogType.Success: color = Brushes.Green; break;
                case LogType.Error: color = Brushes.Red; break;
            }

            rtbLog.Dispatcher.Invoke(() =>
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run(message) { Foreground = color });
                rtbLog.Document.Blocks.Add(paragraph);
                rtbLog.ScrollToEnd();
            });
        }

        private void ClearLog()
        {
            rtbLog.Document.Blocks.Clear();
        }

        private const int SystemHandleInformation = 16;

        private const uint PROCESS_DUP_HANDLE = 0x0040;
        private const uint DUPLICATE_SAME_ACCESS = 0x2;
        private const uint DUPLICATE_CLOSE_SOURCE = 0x1;

        private const int ObjectNameInformation = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE
        {
            public int ProcessId;
            public byte ObjectTypeNumber;
            public byte Flags;
            public ushort Handle;
            public IntPtr Object;
            public uint GrantedAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(
            int systemInformationClass,
            IntPtr systemInformation,
            int systemInformationLength,
            out int returnLength);

        [DllImport("ntdll.dll")]
        private static extern int NtDuplicateObject(
            IntPtr sourceProcessHandle,
            IntPtr sourceHandle,
            IntPtr targetProcessHandle,
            out IntPtr targetHandle,
            uint desiredAccess,
            uint attributes,
            uint options);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryObject(
            IntPtr handle,
            int objectInformationClass,
            IntPtr objectInformation,
            int objectInformationLength,
            out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Tenta matar o Mutex em session 1, depois 2, depois 3.
        // Retorna true se encontrar e fechar em alguma delas.

        private bool TryKillMutexInSessions(int targetProcessId)
        {
            // GUID do Mutex
            string guid = "{071784A2-EE35-4e6a-92D0-6E7A4B985171}";

            // Sessões
            string s1 = $@"\Sessions\1\BaseNamedObjects\{guid}";
            string s2 = $@"\Sessions\2\BaseNamedObjects\{guid}";
            string s3 = $@"\Sessions\3\BaseNamedObjects\{guid}";

            if (TryKillMutex(targetProcessId, s1)) return true;
            if (TryKillMutex(targetProcessId, s2)) return true;
            if (TryKillMutex(targetProcessId, s3)) return true;

            return false;
        }

        // Tenta encontrar e fechar o Mutex (targetMutex) no processo (targetProcessId).
        // Retorna true se conseguir fechar, false caso contrário.

        private bool TryKillMutex(int targetProcessId, string targetMutex)
        {
            // Log($"[INFO] Verificando Mutex: {targetMutex}", LogType.Info);

            IntPtr processHandle = OpenProcess(PROCESS_DUP_HANDLE, false, targetProcessId);
            if (processHandle == IntPtr.Zero)
            {
                Log("[ERRO] Falha ao abrir o processo alvo (OpenProcess). Permissões?", LogType.Error);
                return false;
            }

            bool closed = false;
            try
            {
                closed = EnumerateAndCloseMutex(processHandle, targetProcessId, targetMutex);
            }
            finally
            {
                CloseHandle(processHandle);
            }

            return closed;
        }

        // Faz a enumeração dos handles e fecha se encontrar o Mutex "targetMutex".

        private bool EnumerateAndCloseMutex(IntPtr processHandle, int targetPid, string targetMutex)
        {
            int bufferSize = 0x100000;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            try
            {
                int returnLength;
                int status;

                while ((status = NtQuerySystemInformation(SystemHandleInformation, buffer, bufferSize, out returnLength)) == -1073741820)
                {
                    Marshal.FreeHGlobal(buffer);
                    bufferSize = returnLength;
                    buffer = Marshal.AllocHGlobal(bufferSize);
                }

                if (status != 0)
                {
                    Log($"[ERRO] NtQuerySystemInformation falhou (status=0x{status:X8}).", LogType.Error);
                    return false;
                }

                int handleCount = Marshal.ReadInt32(buffer);
                IntPtr pHandle = buffer + 4;

                bool foundAndClosed = false;

                for (int i = 0; i < handleCount; i++)
                {
                    var sysHandle = Marshal.PtrToStructure<SYSTEM_HANDLE>(pHandle);

                    if (sysHandle.ProcessId == targetPid)
                    {
                        // Duplicar localmente para ler o nome
                        IntPtr localHandle;
                        int dupStatus = NtDuplicateObject(
                            processHandle,
                            (IntPtr)sysHandle.Handle,
                            Process.GetCurrentProcess().Handle,
                            out localHandle,
                            0,
                            0,
                            DUPLICATE_SAME_ACCESS);

                        if (dupStatus == 0 && localHandle != IntPtr.Zero)
                        {
                            if (IsHandleMatchingMutex(localHandle, targetMutex))
                            {
                                Log("[INFO] Mutex detectado! Tentando fechar no processo remoto...", LogType.Info);

                                // Fecha de fato no processo remoto
                                int closeStatus = NtDuplicateObject(
                                    processHandle,
                                    (IntPtr)sysHandle.Handle,
                                    IntPtr.Zero,
                                    out _,
                                    0,
                                    0,
                                    DUPLICATE_CLOSE_SOURCE);

                                if (closeStatus == 0)
                                {
                                    //Log("[SUCESSO] Mutex fechado (handle remoto)!", LogType.Success);
                                    foundAndClosed = true;
                                }
                                else
                                {
                                    Log($"[ERRO] Falha ao fechar handle remoto (0x{closeStatus:X8}).", LogType.Error);
                                }

                                // Fecha nossa duplicata
                                CloseHandle(localHandle);
                                break;
                            }

                            CloseHandle(localHandle);
                        }
                    }

                    if (foundAndClosed) break;
                    pHandle += Marshal.SizeOf<SYSTEM_HANDLE>();
                }

                return foundAndClosed;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private bool IsHandleMatchingMutex(IntPtr handle, string targetMutex)
        {
            IntPtr nameBuffer = Marshal.AllocHGlobal(0x2000);
            try
            {
                int nameLength;
                int qStatus = NtQueryObject(handle, ObjectNameInformation, nameBuffer, 0x2000, out nameLength);
                if (qStatus == 0)
                {
                    UNICODE_STRING uni = Marshal.PtrToStructure<UNICODE_STRING>(nameBuffer);
                    if (uni.Length > 0 && uni.Buffer != IntPtr.Zero)
                    {
                        string name = Marshal.PtrToStringUni(uni.Buffer, uni.Length / 2);
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name.Equals(targetMutex, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(nameBuffer);
            }
            return false;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
