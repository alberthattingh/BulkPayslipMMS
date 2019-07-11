using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EvoPdf.PdfToImage;
using EvoPdf.PdfSplit;
using EvoPdf.PdfToText;
using System.Drawing;
using System.Drawing.Imaging;

namespace BulkPayslipMMS
{
    public class Program
    {
        public static readonly string EvoPdfLicenseKey = "sz0uPCksPCgkLjwrMiw8Ly0yLS4yJSUlJTws";
        public static readonly XNamespace myns = "https://mmsapi.gsm.co.za/";
        public static readonly string PathToFilesDirectory = @"C:\Work\BulkPayslipMMS\BulkPayslipMMS\ResourceFiles\";
        public static readonly string PayslipFilename = @"CO103_02022019_S2_Synfuels_Late Engagements.pdf";

        public static void Main(string[] args)
        {
            SendBulkMMSs();
        }

        public static void SendBulkMMSs()
        {
            string[] employees = GetEmployeeNumbers();

            GeneratePayslipFiles(PayslipFilename);
            foreach (string employee in employees)
            {
                SendMMSToEmployee(employee);
            }
        }

        public static XDocument GetNewSoapRequestXML(string cellNumber, string imageToSend)
        {
            XNamespace ns = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

            XDocument soapRequest = new XDocument(
                new XDeclaration("1.0", "UTF-8", "no"),
                new XElement(ns + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                    new XAttribute(XNamespace.Xmlns + "soap", ns),
                    new XElement(ns + "Body",
                        new XElement(myns + "SendStandardMMS",
                            new XElement(myns + "username", "amt123"),
                            new XElement(myns + "password", "glenys159712"),
                            // new XElement(myns + "subject", "Test"),
                            new XElement(myns + "numbers",
                                new XElement(myns + "string", cellNumber)), // , new XElement(myns + "string", "0761833376")
                            new XElement(myns + "textData", "Here is your payslip!"),
                            new XElement(myns + "imageType", "png"),
                            new XElement(myns + "imageData", imageToSend), // Add image here: ImageToBase64("path/to/image.PNG")
                            new XElement(myns + "soundType", "mp3"),
                            new XElement(myns + "soundData", "")
                        )
                    )
                ));

            return soapRequest;
        }

        public static void SendMMSToEmployee(string employeeNumber)
        {
            XDocument soapRequest = GetNewSoapRequestXML(GetEmployeeCellNumber(employeeNumber),
                                                        GetImageToSendByEmployeeNumber(employeeNumber));

            try
            {
                using (var client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip }))
                {
                    var request = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("https://mmsapi.gsm.co.za/service.asmx"),
                        Method = HttpMethod.Post,
                        Content = new StringContent(soapRequest.ToString(), Encoding.UTF8, "text/xml")
                    };



                    request.Headers.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                    request.Headers.Add("SOAPAction", "https://mmsapi.gsm.co.za/SendStandardMMS");

                    HttpResponseMessage response = client.SendAsync(request).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Error: " + response.StatusCode);
                        // throw new Exception();
                    }

                    Task<Stream> streamTask = response.Content.ReadAsStreamAsync();
                    Stream stream = streamTask.Result;
                    var sr = new StreamReader(stream);
                    var soapResponse = XDocument.Load(sr);
                    Console.WriteLine(soapResponse);

                    var xml = soapResponse.Descendants(myns + "SendStandardMMSResponse").FirstOrDefault().ToString();

                    // Output xml response
                    Console.WriteLine(xml);

                    // Delete the newly created PNG and PDF files
                    DeleteNewFilesByEmployeeNumber(employeeNumber);
                }
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is TaskCanceledException)
                {
                    Console.WriteLine(ex.InnerException);
                }
                else
                {
                    Console.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static string GetImageToSendByEmployeeNumber(string employeeNumber)
        {
            string pathToPDFFile = string.Format("{0}{1}.pdf", PathToFilesDirectory, employeeNumber);
            return ImageToBase64(ConvertPDFToPNG(pathToPDFFile));
        }

        public static string ConvertPDFToPNG(string path)
        {
            // Assuming PDF file only consists of one page
            string filename = Path.GetFileNameWithoutExtension(path);

            PdfToImageConverter converter = new PdfToImageConverter
            {
                LicenseKey = EvoPdfLicenseKey
            };

            Image image = converter.ConvertPdfPagesToImage(path)[0].ImageObject;
            string outputPath = string.Format(@"{0}{1}.PNG", PathToFilesDirectory, filename);
            SaveImageWithReducedSize(image, outputPath);

            return outputPath;
        }

        public static void SaveImageWithReducedSize(Image image, string outputPath)
        {
            int MAX_WIDTH = 832;
            int MAX_HEIGHT = 1248;
            Bitmap bitmap = new Bitmap(image, new Size(MAX_WIDTH, MAX_HEIGHT));
            bitmap.Save(outputPath, ImageFormat.Png);
        }

        public static void GeneratePayslipFiles(string originalPayslipFileName)
        {
            PDFSplitManager splitter = new PDFSplitManager()
            {
                LicenseKey = EvoPdfLicenseKey
            };

            string tempOutputFile = string.Format("{0}EmployeePayslip.pdf", PathToFilesDirectory);
            splitter.ExtractPagesFromFileToFile(PathToFilesDirectory + originalPayslipFileName, tempOutputFile, 1, 1, true);

            // string[] payslipFiles = Directory.GetFiles(PathToFilesDirectory, "EmployeePayslip*.pdf");
            string[] employeeNumbers = GetEmployeeNumbers();

            PdfToTextConverter converter = new PdfToTextConverter
            {
                LicenseKey = EvoPdfLicenseKey
            };

            FindTextLocation[] locations;
            string outputFile;
            foreach (string employee in employeeNumbers)
            {
                foreach (string file in Directory.GetFiles(PathToFilesDirectory, "EmployeePayslip*.pdf"))
                {
                    locations = converter.FindText(file, employee, true, true);
                    if (locations.Length >= 1)
                    {
                        outputFile = string.Format("{0}{1}.pdf", PathToFilesDirectory, employee);

                        try
                        {
                            File.Copy(file, outputFile);
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        break;
                    }
                }
            }
        }

        public static void DeleteNewFilesByEmployeeNumber(string employeeNumber)
        {
            string pathToPNGFile = string.Format(@"{0}{1}.PNG", PathToFilesDirectory, employeeNumber);
            File.Delete(pathToPNGFile);

            string pathToPDFFile = string.Format(@"{0}{1}.pdf", PathToFilesDirectory, employeeNumber);
            File.Delete(pathToPDFFile);
        }

        public static string ImageToBase64(string path)
        {
            // Image must be PNG
            // Total MMS size must be <= 150kb
            byte[] imageArray = File.ReadAllBytes(path);
            return Convert.ToBase64String(imageArray);
        }




        // These methods don't have to be used in the App.
        // A version of these methods can be used if the method bodies are changed to get info from the DB
    
        /*
         * Change this method to return an array of employee numbers from database
         */
        public static string[] GetEmployeeNumbers()
        {
            return new string[] { "6508_890914", "6508_930907" };
        }

        /*
         * Change this method to return the cell number from the database that corresponds to the employee number
         */
        public static string GetEmployeeCellNumber(string employeeNumber)
        {
            if (employeeNumber.Equals(6508_890914))
                return "0761833376";
            else if (employeeNumber.Equals(6508_930907))
                return "0768807196";
            else
                return "0761833376";
        }
    }
}