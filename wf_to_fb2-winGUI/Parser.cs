using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using System.Net;
using System.Collections.ObjectModel;

namespace wf_to_fb2
{
    class Parser
    {
        
        public static ObservableCollection<Vol> Get_Tree(string URL)
        {
            string HTML_string = Get_Page(URL);
            ObservableCollection<Vol> volumes = new ObservableCollection<Vol>();
            HtmlParser parser = new HtmlParser();
            var html = parser.Parse(HTML_string);
            var nodes = html.QuerySelector(".post-content");
            for (int i = 0; i < nodes.Children.Length; i++)
            {
                if (nodes.Children[i].NodeName == "UL")
                {
                    Vol vol = new Vol();
                    //get vol name
                    for (int i_inc = i; i_inc > 0; i_inc--)
                    {
                        if (nodes.Children[i_inc].NodeName == "P")
                        {
                            vol.Text = nodes.Children[i_inc].TextContent.Replace("\n", "");
                            break;
                        }
                    }
                    //make chapters list
                    ObservableCollection<Chapter> chapters = new ObservableCollection<Chapter>();
                    foreach (var node in nodes.Children[i].Children)
                    {
                        var chapter = new Chapter();
                        chapter.Checked = false;
                        chapter.Text = node.TextContent.Replace("\n", "");
                        try
                        {
                            chapter.URL = node.FirstElementChild.GetAttribute("href").Replace("\n", "");
                        }
                        catch (Exception)
                        {

                            chapter.URL = "";
                        }
                        
                        chapters.Add(chapter);
                    }
                    vol.Chapters = chapters;
                    vol.ThreeState = false;
                    vol.Checked = false;
                    volumes.Add(vol);
                }
            }
            for (int i = 0; i < volumes.Count; i++)
            {
                foreach (var chapter in volumes[i].Chapters)
                {
                    chapter.InVolume = i;
                }
            }
            return volumes;
        }

        public static string Get_Page(string url)
        {
            string html;
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                html = client.DownloadString(url);
            }
            return html;
        }
        public static string Get_BookName(string html_string)
        {
            HtmlParser parser = new HtmlParser();
            var html = parser.Parse(html_string);
            string bookname = html.QuerySelector(".post-title").TextContent;
            return bookname;
        }
        public static XDocument MakeFB2Sample(string FName, string LName, string BookName, DateTime Time, string CoverImgB64)
        {
            XNamespace xmlns = "http://www.gribuser.ru/xml/fictionbook/2.0";
            XDocument fb2 = new XDocument();
            XElement date = new XElement("date",
                                         new XAttribute("value", Time.ToString("yyyy-MM-dd")),
                                         Time.ToString("dd.MM.yyyy"));

            XElement titleInfo = new XElement("title-info",
                                     new XElement("genre", "novel"),
                                     new XElement("author",
                                         new XElement("first-name", FName),
                                         new XElement("last-name", LName)),
                                     new XElement("book-title", BookName),
                                     date,
                                     new XElement("lang", "ru")
                                 );
            XElement documentInfo = new XElement("document-info",
                                        new XElement("program-used", "wt to fb2 by mixa3607"),
                                        new XElement("src-url", "https://wolftales.ru"),
                                        new XElement("version", "1.0")
                                    );
            XElement description = new XElement("description", titleInfo, documentInfo);
            XElement fictionBook = new XElement(xmlns + "FictionBook",
                                       new XAttribute("xmlns", xmlns.NamespaceName),
                                       new XAttribute(XNamespace.Xmlns + "l", "http://www.w3.org/1999/xlink"),
                                       //new XAttribute(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"),
                                       description,
                                       new XElement("body", new XElement("title", new XElement("p", BookName)))
                                   );
            if (CoverImgB64 != null)
            {
                XNamespace l = "http://www.w3.org/1999/xlink";
                titleInfo.Add(new XElement("coverpage", new XElement("image", new XAttribute(l + "href", "#cover.jpg"))));
                fictionBook.Add(new XElement("binary", new XAttribute("id", "cover.jpg"), new XAttribute("content-type", "image/jpeg"), CoverImgB64));
            }
            fb2.Add(fictionBook);

            return fb2;
        }
        public static XElement Parse(string html_string)
        {
            HtmlParser parser = new HtmlParser();
            var html = parser.Parse(html_string);

            var title = html.QuerySelector(".post-title");
            var section = new XElement("section", new XElement("title", new XElement("p", title.TextContent)));


            var text = html.QuerySelectorAll(".post-content:not(.comment-content) > *:not(div)");

            for (int i = 2; i < text.Length; i++)
            {
                switch (text[i].LocalName)
                {
                    case "p": var add = Parse_P(text[i]); if (add.Count == 0) continue; section.Add(new XElement("p", add)); break;
                    case "table": section.Add(Parse_Table(text[i])); break;
                    case "hr": section.Add(new XElement("empty-string")); break;
                }
            }
            return section;
        }
        static List<object> Parse_P(IElement html)
        {
            List<object> elements = new List<object>();
            var nodes = html.ChildNodes;
            foreach (var node in nodes)
            {
                if (string.IsNullOrWhiteSpace(node.TextContent))
                {
                    continue;
                }
                if (node.ChildNodes.Length <= 1)
                {

                    switch (node.NodeName)
                    {
                        case "P": elements.Add(node.TextContent); break;
                        case "SPAN": elements.Add(node.TextContent); break;
                        case "#text": elements.Add(node.TextContent); break;
                        case "EM": elements.Add(new XElement("emphasis", node.TextContent)); break;
                        case "STRONG": elements.Add(new XElement("strong", node.TextContent)); break;
                    }
                }
                else
                {
                    string nodename = null;
                    switch (node.NodeName)
                    {
                        case "SPAN": break;
                        case "EM": nodename = "emphasis"; break;
                        case "STRONG": nodename = "strong"; break;
                    }
                    if (nodename == null)
                    {
                        elements.Add(Parse_P((IElement)node));
                    }
                    else
                    {
                        elements.Add(new XElement(nodename, Parse_P((IElement)node)));
                    }
                }

            }
            return elements;
        }
        public static XDocument Linker(XDocument sample, List<XElement> chapters)
        {
            XNamespace xmlns = "http://www.gribuser.ru/xml/fictionbook/2.0";
            var sample_body = sample.Element(xmlns + "FictionBook").Element("body");
            foreach (var chapter in chapters)
            {
                sample_body.Add(chapter);
            }


            foreach (var element in sample.Descendants().ToList())
            {
                element.Name = xmlns + element.Name.LocalName;
            }
            return sample;
        }
        static List<XElement> Parse_Table(IElement html)
        {
            List<XElement> tables = new List<XElement>();
            var rows = html.QuerySelectorAll("tbody > tr");
            //Console.WriteLine(rows.Length);
            int prevous_count = 0;
            int current_count;
            foreach (var row in rows)
            {
                XElement table;
                var all_p = row.Children;

                //Console.WriteLine(all_p.Length);

                current_count = all_p.Length;
                var tr = new XElement("tr");
                foreach (var p in all_p)
                {
                    var add = Parse_P(p);
                    if (add.Count == 0)
                    {
                        continue;
                    }
                    //Console.WriteLine("count " + add.Count);
                    tr.Add(new XElement("td", add));
                }
                if (prevous_count == current_count)
                {
                    tables.Last().Add(tr);
                }
                else
                {
                    table = new XElement("table");
                    table.Add(tr);
                    tables.Add(table);
                }

                prevous_count = current_count;
            }
            return tables;
        }
        public static int Save(string path, XDocument content)
        {
            try
            {
                content.Save(path);
                return 0;
            }
            catch
            {
                return 2;
            }
        }
    }
}
