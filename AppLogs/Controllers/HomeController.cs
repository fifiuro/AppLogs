using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Transactions;
using AppLogs.Models;
using Microsoft.AspNetCore.Mvc;
using Rotativa.AspNetCore;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace AppLogs.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult UploadLogFileWithFecha(DateTime startDate, DateTime endDate)
        {
            try
            {
                var file = Request.Form.Files[0]; // Obtener el archivo de la solicitud

                if (file != null && file.Length > 0)
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        List<Transaction> transactions = new List<Transaction>();

                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            var values = line.Split(',');

                            // Procesar los valores de la línea
                            var date = DateTime.Parse(values[0]);

                            // Verificar si la fecha está dentro del rango especificado
                            if (date >= startDate && date <= endDate)
                            {
                                var time = TimeSpan.Parse(values[1]);
                                var type = values[2];
                                var amount = decimal.Parse(values[3]);

                                // Crear una transacción y agregarla a la lista
                                transactions.Add(new Transaction
                                {
                                    Date = date,
                                    Time = time,
                                    Type = type,
                                    Amount = amount
                                });
                            }
                        }

                        // Calcular el total de depósitos
                        decimal totalDeposits = transactions
                            .Where(t => t.Type == "Deposito")
                            .Sum(t => t.Amount);

                        // Calcular el total de retiros
                        decimal totalWithdrawals = transactions
                            .Where(t => t.Type == "Retiro")
                            .Sum(t => t.Amount);

                        // Calcular el saldo
                        decimal balance = totalDeposits - totalWithdrawals;

                        ViewBag.FechaInicio = startDate;
                        ViewBag.FechaFin = endDate;
                        ViewBag.TotalDeposits = totalDeposits;
                        ViewBag.TotalWithdrawals = totalWithdrawals;
                        ViewBag.Balance = balance;

                        return View("index", transactions);
                    }
                }
                else
                {
                    ViewBag.Message = "Por favor seleccione un archivo.";
                    return View("Error");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error al procesar el archivo de log: " + ex.Message;
                return View("Error");
            }
        }

        public IActionResult ExportToExcel(List<Transaction> model)
        {
            Console.WriteLine(model.ToArray());
            // Crear un MemoryStream para escribir el contenido del Excel
            using (MemoryStream stream = new MemoryStream())
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Establecer el contexto de la licencia (necesario para EPPlus)

                using (ExcelPackage package = new ExcelPackage(stream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Transacciones");

                    // Encabezados de columnas
                    worksheet.Cells[1, 1].Value = "Fecha";
                    worksheet.Cells[1, 2].Value = "Hora";
                    worksheet.Cells[1, 3].Value = "Tipo";
                    worksheet.Cells[1, 4].Value = "Monto";

                    // Datos de transacciones
                    int row = 2;
                    foreach (var transaction in model)
                    {
                        worksheet.Cells[row, 1].Value = transaction.Date.ToShortDateString();
                        worksheet.Cells[row, 2].Value = transaction.Time;
                        worksheet.Cells[row, 3].Value = transaction.Type;
                        worksheet.Cells[row, 4].Value = transaction.Amount;
                        row++;
                    }

                    // Autoajustar anchos de columnas
                    worksheet.Cells.AutoFitColumns(0);

                    // Guardar el paquete de Excel en el MemoryStream
                    package.Save();
                }

                // Devolver el archivo Excel como un archivo descargable
                byte[] excelBytes = stream.ToArray();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "transacciones.xlsx");
            }
        }
    }
}

public class Transaction
{
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
}