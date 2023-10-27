using System;
using System.Collections.Generic;
using Maistaxi.Business.Model;
using Maistaxi.Business.Model.Publicacao;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Maistaxi.Business.Pdf {
    public class PdfFileWriter {

        internal static void WriteToPdf(string caminhoArquivo, Sorteio sorteio, ListaPub lista)
        {
            DateTime dataHoraImpressao = new DateTime();
            List<String> linhas = new List<String>();
            using (FileStream fileStream = new FileStream(caminhoArquivo, FileMode.Create))
            {
                PdfWriter writer = null;
                using (Document document = new Document(PageSize.A4))
                {

                    document.SetMargins(20, 20, 20, 40);
                    document.AddCreationDate();
                    writer = PdfWriter.GetInstance(document, fileStream);
                    writer.PageEvent = new CustomPdfPageEventHelper(lista.Nome);
                    document.Open();
                    dataHoraImpressao = DateTime.Now;
                    try
                    {
                        string dbDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        Uri enderecoLogoUrbes = new Uri($"{dbDirectory}\\URBES.jpg");
                        Uri enderecoLogoPMS = new Uri($"{dbDirectory}\\logotipo.jpg");
                        Jpeg logoUrbes = new Jpeg(enderecoLogoUrbes);
                        Jpeg logoPMS = new Jpeg(enderecoLogoPMS);
                        logoPMS.ScalePercent(35);
                        logoUrbes.ScalePercent(25);
                        //logoPMS.SetAbsolutePosition(0, 0);
                        logoUrbes.Alignment = Element.ALIGN_RIGHT;
                        logoPMS.SetAbsolutePosition(30, 803);
                        document.Add(logoUrbes);
                        document.Add(logoPMS);
                    } catch(Exception e)
                    {
                        
                    }
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    Paragraph p = new Paragraph();
                    p.Alignment = Element.ALIGN_CENTER;
                    p.Font = headerFont;
                    linhas.Add("PREFEITURA DE SOROCABA \n");
                    p.Add("PREFEITURA DE SOROCABA \n");
                    linhas.Add(string.Format("{0}\n", sorteio.Nome.ToUpper()));
                    p.Add(string.Format("{0}\n", sorteio.Nome.ToUpper()));
                    linhas.Add(string.Format("{0:00} - {1}\n", lista.IdLista, lista.Nome.ToUpper()));
                    p.Add(string.Format("{0:00} - {1}\n", lista.IdLista, lista.Nome.ToUpper()));
                    linhas.Add(string.Format("Semente de Sorteio: {0} ({1})\n\n", lista.SementeSorteio, lista.FonteSementeSorteio));
                    p.Add(string.Format("Semente de Sorteio: {0} ({1})\n\n", lista.SementeSorteio, lista.FonteSementeSorteio));

                    if (lista.Candidatos.Count == 0)
                    {
                        p.Add("Não houveram inscritos suficientes para esta lista\n\n");
                        linhas.Add("Não houveram inscritos suficientes para esta lista");
                        document.Add(p);
                    }
                    else {
                        document.Add(p);
                        PdfPTable table = new PdfPTable(4);
                        table.WidthPercentage = 100f;
                        table.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        table.SetWidths(new float[] { 1f, 3f, 7f, 2f });
                        table.HeaderRows = 1;

                        table.AddCell(new PdfPCell(new Phrase("Nº", headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });

                        table.AddCell(new PdfPCell(new Phrase("CPF", headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });

                        table.AddCell(new PdfPCell(new Phrase("NOME", headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });

                        table.AddCell(new PdfPCell(new Phrase("INSCRIÇÃO", headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });

                        linhas.Add(String.Concat("Nº   ", "CPF                 ", "NOME,", "INSCRIÇÃO"));

                        foreach (CandidatoPub candidato in lista.Candidatos)
                        {
                            table.AddCell(string.Format("{0:000}", candidato.IdCandidato));
                            table.AddCell(candidato.Cpf);
                            table.AddCell(candidato.Nome.ToUpper());
                            table.AddCell(string.Format("{0:000000}", candidato.IdInscricao.ToString()));
                            linhas.Add(String.Concat(string.Format("{0:000}", candidato.IdCandidato), " ", candidato.Cpf, " ", candidato.Nome.ToUpper(), ", ", string.Format("{0:000000}", candidato.IdInscricao.ToString())));
                        }

                        document.Add(table);
                    }
                }
                System.Drawing.Printing.PrintDocument printer = new System.Drawing.Printing.PrintDocument();
                printer.PrintPage += delegate (object envio, System.Drawing.Printing.PrintPageEventArgs eArgs)
                {
                    for (int ix = 0; ix < linhas.Count; ix++)
                    {
                        if (ix < 4)
                        {
                            eArgs.Graphics.DrawString(linhas[ix], new System.Drawing.Font("HELVETICA_BOLD", 8, System.Drawing.FontStyle.Bold), new System.Drawing.SolidBrush(System.Drawing.Color.Black), new System.Drawing.PointF(300 - (linhas[ix].Length), ix * 15));
                        } else
                        {
                            eArgs.Graphics.DrawString(linhas[ix].Split(',')[0], new System.Drawing.Font("HELVETICA_BOLD", 8), new System.Drawing.SolidBrush(System.Drawing.Color.Black), new System.Drawing.RectangleF(0, ix * 15, 650, 20));
                            eArgs.Graphics.DrawString(linhas[ix].Split(',')[1], new System.Drawing.Font("HELVETICA_BOLD", 8), new System.Drawing.SolidBrush(System.Drawing.Color.Black), new System.Drawing.RectangleF(650, ix * 15, 100, 20));
                            eArgs.Graphics.DrawString(linhas[ix].Split(',')[2], new System.Drawing.Font("HELVETICA_BOLD", 8), new System.Drawing.SolidBrush(System.Drawing.Color.Black), new System.Drawing.RectangleF(750, ix * 15, 100, 20));
                        }
                    }
                    //github.com/tisorocaba
                    eArgs.Graphics.DrawString(lista.Nome, new System.Drawing.Font("HELVETICA_BOLD", 8), new System.Drawing.SolidBrush(System.Drawing.Color.Gray), new System.Drawing.RectangleF(0, 1080, 700, 20));
                    eArgs.Graphics.DrawString(dataHoraImpressao.ToString(), new System.Drawing.Font("HELVETICA_BOLD", 8), new System.Drawing.SolidBrush(System.Drawing.Color.Gray), new System.Drawing.RectangleF(700, 1080, 200, 20));
                };
                try
                {
                    printer.Print();
                } catch(Exception ex)
                {

                }
                writer.Close();
            }
        }

        internal static void WriteSorteadosToPdf(string caminhoArquivo, Sorteio sorteio, ListaPub lista) {
            using (FileStream fileStream = new FileStream(caminhoArquivo, FileMode.Create)) {
                PdfWriter writer = null;
                using (Document document = new Document(PageSize.A4)) {

                    document.SetMargins(20, 20, 20, 40);
                    document.AddCreationDate();
                    writer = PdfWriter.GetInstance(document, fileStream);
                    writer.PageEvent = new CustomPdfPageEventHelper(lista.Nome);
                    document.Open();

                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6);
                    Paragraph p = new Paragraph();
                    p.Alignment = Element.ALIGN_CENTER;
                    p.Font = headerFont;
                    p.Add("PREFEITURA DE SOROCABA \n");
                    p.Add("RELAÇÃO DE SORTEADOS PARA ALVARÁ DE AUTORIZAÇÃO DE TÁXI \n\n");
                    document.Add(p);

                    PdfPTable table = new PdfPTable(6);
                    table.WidthPercentage = 100f;
                    table.DefaultCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SetWidths(new float[] { 3f, 7f, 2f, 3f, 7f, 2f });
                    table.HeaderRows = 1;

                    table.AddCell(new PdfPCell(new Phrase("CPF", headerFont)) {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("NOME", headerFont)) {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("INSCRIÇÃO", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });
                    table.AddCell(new PdfPCell(new Phrase("CPF", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("NOME", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    table.AddCell(new PdfPCell(new Phrase("INSCRIÇÃO", headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });

                    foreach (CandidatoPub candidato in lista.Candidatos) {
                        table.AddCell(new Phrase(candidato.Cpf, headerFont));
                        table.AddCell(new Phrase(candidato.Nome.ToUpper(), headerFont));
                        table.AddCell(new Phrase(string.Format("{0:000000}", candidato.IdInscricao.ToString()), headerFont));
                    }

                    if (lista.Candidatos.Count % 2 > 0)
                    {
                        table.AddCell(String.Empty);
                        table.AddCell(String.Empty);
                        table.AddCell(String.Empty);
                    }

                    document.Add(table);
                }
                writer.Close();
            }
        }
    }
}
