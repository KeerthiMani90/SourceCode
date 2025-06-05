// Decompiled with JetBrains decompiler
// Type: PSUpload.Program
// Assembly: PSUpload, Version=2.0.6828.36913, Culture=neutral, PublicKeyToken=null
// MVID: 5FA42BE7-0A14-4E52-BFFB-5F5B65841F7D
// Assembly location: C:\Users\maniamar\Downloads\PSUpload\PSUpload.exe

using System;
using System.Windows.Forms;

namespace PSUpload
{
  internal static class Program
  {
    [STAThread]
    private static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run((Form) new ProductionsiteUploadFrm());
    }
  }
}
