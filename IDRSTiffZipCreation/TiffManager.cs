// Decompiled with JetBrains decompiler
// Type: TiffCreation.TiffManager
// Assembly: TiffCreation, Version=2.0.6828.36969, Culture=neutral, PublicKeyToken=null
// MVID: D9DCA5D8-9D36-4095-9BDF-F0AA3BBAA005
// Assembly location: C:\Users\keerthim\Desktop\Tri\Tri\TiffCreation.exe

using Generic.Util;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDRSTiffZipCreationConversion
{
  public class TiffManager : IDisposable
  {
    private string _imageFileName;
    private int _pageNumber;
    private readonly Image _image;
    private string _tempWorkingDir;

    public string ImageFileName
    {
      get
      {
        return this._imageFileName;
      }
      set
      {
        this._imageFileName = value;
      }
    }

    public string TempWorkingDir
    {
      get
      {
        return this._tempWorkingDir;
      }
      set
      {
        this._tempWorkingDir = value;
      }
    }

    public int PageNumber
    {
      get
      {
        return this._pageNumber;
      }
    }

    public TiffManager(string imageFileName)
    {
      this._imageFileName = imageFileName;
      this._image = Image.FromFile(this._imageFileName);
      this.GetPageNumber();
    }

    public TiffManager()
    {
    }

    private void GetPageNumber()
    {
      this._pageNumber = this._image.GetFrameCount(new FrameDimension(this._image.FrameDimensionsList[0]));
    }

    private string GetFileNameStartString()
    {
      int num1 = this._imageFileName.LastIndexOf(".", StringComparison.Ordinal);
      int num2 = this._imageFileName.LastIndexOf("\\", StringComparison.Ordinal);
      return this._imageFileName.Substring(num2 + 1, num1 - num2 - 1);
    }

    public ArrayList SplitTiffImage(string outPutDirectory, EncoderValue format)
    {
      string str = outPutDirectory + "\\" + this.GetFileNameStartString();
      ArrayList arrayList = new ArrayList();
      FrameDimension dimension = new FrameDimension(this._image.FrameDimensionsList[0]);
      Encoder compression = Encoder.Compression;
      int frameIndex = 0;
      for (int index = 0; index < this._pageNumber; ++index)
      {
        this._image.SelectActiveFrame(dimension, frameIndex);
        EncoderParameters encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(compression, (long) format);
        ImageCodecInfo encoderInfo = this.GetEncoderInfo("image/tiff");
        string filename = string.Format("{0}{1}.TIF", (object) str, (object) index.ToString((IFormatProvider) CultureInfo.InvariantCulture));
        this._image.Save(filename, encoderInfo, encoderParams);
        arrayList.Add((object) filename);
        ++frameIndex;
      }
      return arrayList;
    }

    public void JoinTiffImages(string[] imageFiles, string outFile, EncoderValue compressEncoder)
    {
      if (imageFiles.Length == 1)
      {
        File.Copy(imageFiles[0], outFile, true);
      }
      else
      {
        Encoder saveFlag = Encoder.SaveFlag;
        EncoderParameters encoderParams = new EncoderParameters(2);
        encoderParams.Param[0] = new EncoderParameter(saveFlag, 18L);
        encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, (long) compressEncoder);
        Bitmap bitmap1 = (Bitmap) null;
        int num = 0;
        ImageCodecInfo encoderInfo = this.GetEncoderInfo("image/tiff");
        foreach (string imageFile in imageFiles)
        {
          if (num == 0)
          {
            bitmap1 = (Bitmap) Image.FromFile(imageFile);
            bitmap1.Save(outFile, encoderInfo, encoderParams);
          }
          else
          {
            encoderParams.Param[0] = new EncoderParameter(saveFlag, 23L);
            Bitmap bitmap2 = (Bitmap) Image.FromFile(imageFile);
            if (bitmap1 != null)
              bitmap1.SaveAdd((Image) bitmap2, encoderParams);
            bitmap2.Dispose();
          }
          if (num == imageFiles.Length - 1)
          {
            encoderParams.Param[0] = new EncoderParameter(saveFlag, 20L);
            if (bitmap1 != null)
              bitmap1.SaveAdd(encoderParams);
          }
          ++num;
        }
        if (bitmap1 == null)
          return;
        bitmap1.Dispose();
      }
    }

    public void JoinTiffImages(ArrayList imageFiles, string outFile, EncoderValue compressEncoder, string BitonalConvert)
    {
      bool flag = false;
      if (imageFiles.Count == 1)
      {
        File.Copy((string) imageFiles[0], outFile, true);
      }
      else
      {
        Encoder saveFlag = Encoder.SaveFlag;
        EncoderParameters encoderParams = new EncoderParameters(2);
        Bitmap bitmap = (Bitmap) Image.FromFile(imageFiles[0].ToString());
        int num1 = 0;
        ImageCodecInfo encoderInfo = this.GetEncoderInfo("image/tiff");
        encoderParams.Param[0] = new EncoderParameter(saveFlag, 18L);
        if (bitmap.PixelFormat == PixelFormat.Format1bppIndexed)
        {
          encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, 4L);
          flag = true;
        }
        else
          encoderParams.Param[1] = new EncoderParameter(Encoder.Compression, 2L);
        foreach (string imageFile in imageFiles)
        {
          if (num1 == 0)
          {
            bitmap = (Bitmap) Image.FromFile(imageFile);
            bitmap.Save(outFile, encoderInfo, encoderParams);
          }
          else
          {
            encoderParams.Param[0] = new EncoderParameter(saveFlag, 23L);
            Bitmap original = (Bitmap) Image.FromFile(imageFile);
            if (((original.PixelFormat == PixelFormat.Format1bppIndexed ? 0 : (BitonalConvert == "Y" ? 1 : 0)) & (flag ? 1 : 0)) != 0)
            {
              original = this.ConvertToBitonal(original);
              int num2 = (int) MessageBox.Show(imageFile);
            }
            bitmap.SaveAdd((Image) original, encoderParams);
            original.Dispose();
          }
          if (num1 == imageFiles.Count - 1)
          {
            encoderParams.Param[0] = new EncoderParameter(saveFlag, 20L);
            bitmap.SaveAdd(encoderParams);
          }
          ++num1;
        }
                bitmap.Dispose();
                encoderParams.Dispose();                
       }
            
    }

    public void RemoveAPage(int pageNumber, EncoderValue compressEncoder, string strFileName)
    {
      ArrayList imageFiles = this.SplitTiffImage(this._tempWorkingDir, compressEncoder);
      string str = string.Format("{0}\\{1}{2}.TIF", (object) this._tempWorkingDir, (object) this.GetFileNameStartString(), (object) pageNumber);
      imageFiles.Remove((object) str);
      this.JoinTiffImages(imageFiles, strFileName, compressEncoder, string.Empty);
    }

    private ImageCodecInfo GetEncoderInfo(string mimeType)
    {
      foreach (ImageCodecInfo imageEncoder in ImageCodecInfo.GetImageEncoders())
      {
        if (imageEncoder.MimeType == mimeType)
          return imageEncoder;
      }
      throw new Exception(mimeType + " mime type not found in ImageCodecInfo");
    }

    public Image GetSpecificPage(int pageNumber)
    {
      MemoryStream memoryStream = (MemoryStream) null;
      Image image = (Image) null;
      try
      {
        memoryStream = new MemoryStream();
        this._image.SelectActiveFrame(new FrameDimension(this._image.FrameDimensionsList[0]), pageNumber);
        this._image.Save((Stream) memoryStream, ImageFormat.Bmp);
        return Image.FromStream((Stream) memoryStream);
      }
      catch (Exception ex)
      {
        if (memoryStream != null)
          memoryStream.Close();
        if (image != null)
          image.Dispose();
        throw;
      }
    }

    public void ConvertTiffFormat(string strNewImageFileName, EncoderValue compressEncoder)
    {
      this.JoinTiffImages(this.SplitTiffImage(this._tempWorkingDir, compressEncoder), strNewImageFileName, compressEncoder, string.Empty);
    }

    public Bitmap ConvertToBitonal(Bitmap original)
    {
      Bitmap bitmap1 = (Bitmap) null;
      try
      {
        Bitmap bitmap2;
        if (original.PixelFormat != PixelFormat.Format32bppArgb)
        {
          bitmap2 = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb);
          bitmap2.SetResolution(original.HorizontalResolution, original.VerticalResolution);
          using (Graphics graphics = Graphics.FromImage((Image) bitmap2))
            graphics.DrawImageUnscaled((Image) original, 0, 0);
        }
        else
          bitmap2 = original;
        BitmapData bitmapdata1 = bitmap2.LockBits(new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int length1 = bitmapdata1.Stride * bitmapdata1.Height;
        byte[] destination = new byte[length1];
        Marshal.Copy(bitmapdata1.Scan0, destination, 0, length1);
        bitmap2.UnlockBits(bitmapdata1);
        bitmap1 = new Bitmap(bitmap2.Width, bitmap2.Height, PixelFormat.Format1bppIndexed);
        bitmap1.SetResolution(original.HorizontalResolution, original.VerticalResolution);
        BitmapData bitmapdata2 = bitmap1.LockBits(new Rectangle(0, 0, bitmap1.Width, bitmap1.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
        int length2 = bitmapdata2.Stride * bitmapdata2.Height;
        byte[] source = new byte[length2];
        int height = bitmap2.Height;
        int width = bitmap2.Width;
        for (int index1 = 0; index1 < height; ++index1)
        {
          int num1 = index1 * bitmapdata1.Stride;
          int index2 = index1 * bitmapdata2.Stride;
          byte num2 = 0;
          int num3 = 128;
          for (int index3 = 0; index3 < width; ++index3)
          {
            if ((int) destination[num1 + 1] + (int) destination[num1 + 2] + (int) destination[num1 + 3] > 500)
              num2 += (byte) num3;
            if (num3 == 1)
            {
              source[index2] = num2;
              ++index2;
              num2 = (byte) 0;
              num3 = 128;
            }
            else
              num3 >>= 1;
            num1 += 4;
          }
          if (num3 != 128)
            source[index2] = num2;
        }
        Marshal.Copy(source, 0, bitmapdata2.Scan0, length2);
        bitmap1.UnlockBits(bitmapdata2);
        if (bitmap2 != original)
          bitmap2.Dispose();
      }
      catch (Exception ex)
      {
        LogFile.WriteErrorLog("TiffManager.CS", "ConvertToBitonal", ex.Message);
      }
      return bitmap1;
    }

    public void Dispose()
    {
      if (this._image != null)
        this._image.Dispose();
      GC.SuppressFinalize((object) this);
    }
  }
}
