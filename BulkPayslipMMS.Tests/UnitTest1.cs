using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BulkPayslipMMS;
using System.Xml.Linq;

namespace BulkPayslipMMS.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestGetNewSoapRequestXML()
        {
            // Arrange
            XNamespace ns = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace myns = "https://mmsapi.gsm.co.za/";

            string testNumber = "0761833376";
            string testImage = "test-base64-image";
            string resultNumber, resultImage;

            // Act
            XDocument xml = Program.GetNewSoapRequestXML(testNumber, testImage);

            resultNumber = xml.Element(ns + "Envelope")
                .Element(ns + "Body")
                .Element(myns + "SendStandardMMS")
                .Element(myns + "numbers")
                .Element(myns + "string").Value;

            resultImage = xml.Element(ns + "Envelope")
                .Element(ns + "Body")
                .Element(myns + "SendStandardMMS")
                .Element(myns + "imageData").Value;

            // Assert
            Assert.AreEqual(testNumber, resultNumber);
            Assert.AreEqual(testImage, resultImage);
        }

        [TestMethod]
        public void TestImageToBase64()
        {
            // Arrange
            string path = @"C:\Work\BulkPayslipMMS\BulkPayslipMMS.Tests\ResourceFiles\ImageToSend.PNG";

            // Act
            string result = Program.ImageToBase64(path);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestConvertPDFToPNG()
        {
            // Arrange
            string path = @"C:\Work\BulkPayslipMMS\BulkPayslipMMS.Tests\ResourceFiles\CO103_02022019_S2_Synfuels_Late Engagements.pdf";

            // Act
            string result = Program.ConvertPDFToPNG(path);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestGetImageToSendByEmployeeNumber()
        {
            System.IO.File.
        }
    }
}
