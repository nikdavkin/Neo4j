using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet;
using static dotnet.GraphDB;

namespace after
{
    public class Example : IDisposable
    {
        private readonly IDriver _driver;
        static async Task<int> Main()
        {
            string task;
            IDriver driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1111"));
            IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
            Console.WriteLine("1 - пробник");
            Console.WriteLine("2 - получить каталоги в которых находится текущая страница");
            Console.WriteLine("3 - переименовать страницу");
            Console.WriteLine("4 - сделать файл readonly для всех пользователей кроме указанного");
            Console.WriteLine("5 - завершить редактирование файла");
            Console.WriteLine("6 - получить список страниц, лежащих в указанном каталоге");
            Console.WriteLine("7 - получить список подкаталогов, лежащих в указанном каталоге");
            Console.WriteLine("8 - получить каталоги, в которых находится текущая траница");
            Console.WriteLine("9 - переименовать каталог");
            Console.WriteLine("10 - добавить новый каталог");
            Console.WriteLine("11 - добавить новую страницу");
            Console.WriteLine("12 - добавить новый тип связи");
            Console.WriteLine("13 - добавить связь между двумя страницами");
            Console.WriteLine("14 - добавить страницу в каталог");
            Console.WriteLine("15 - добавить подкаталог в каталог");
            Console.WriteLine("16 - удалить страницу");
            Console.WriteLine("17 - удалить каталог");
            Console.WriteLine("18 - добавить связь между двумя узлами");
            Console.WriteLine("19 - убрать страницу из каталога");
            Console.WriteLine("20 - убрать подкаталог из каталога");
            Console.WriteLine("21 - вернуть каталог с указанным именем");
            Console.WriteLine("22 - вернуть страницу с указанным именем");
            Console.WriteLine("23 - вернуть связь с указанным именем");
            Console.WriteLine("24 - найти все связи, выходящие из данного узла, и связанные узлы");
            Console.WriteLine("25 - найти все связи, входящие в данный узел, и связанные узлы");
            Console.WriteLine("26 - вернуть страницу с указанным названием");
            Console.WriteLine("27 - вернуть все страницы");
            Console.WriteLine("28 - вернуть каталог с указанным названием");
            Console.WriteLine("29 - вернуть все каталоги");
            Console.WriteLine("30 - вернуть все используемые связи");
            Console.WriteLine("31 - вернуть все связи");
            Console.WriteLine("32 - вернуть все метки");
            Console.WriteLine("0 - выход");
            while (true)
            {
                Page Nowpage = null;
                Catalog Nowcatalog = null;
                if (Nowpage != null)
                {
                    Console.WriteLine("Текущая страница: " + Nowpage.UniqName);
                }
                if (Nowcatalog != null)
                {
                    Console.WriteLine("Текущий каталог: " + Nowcatalog.UniqName);
                }
                Console.Write("Введите номер действия: ");
                task = Console.ReadLine();
                if (task == "1")
                {

                    //IAsyncSession session1 = driver.AsyncSession(o => o.WithDatabase("neo4j"));
                    string name, country, score;
                    Console.WriteLine("Введите имя");
                    name = Console.ReadLine();
                    Console.WriteLine("Введите страну");
                    country = Console.ReadLine();
                    Console.WriteLine("Введите счёт");
                    score = Console.ReadLine();
                    try
                    {
                        IResultCursor cursor = await session.RunAsync("CREATE (ee:Person {name: '" + name + "', from: '" + country + "', kloutScore: " + score + "})");
                        await cursor.ConsumeAsync();
                        //IResultCursor cursor1 = await session1.RunAsync("CREATE (ee:Person {name: 'Tata', from: 'Russia', kloutScore: 100})");
                        //await cursor1.ConsumeAsync();
                    }
                    finally
                    {
                        await session.CloseAsync();
                        //await session1.CloseAsync();
                    }
                    await driver.CloseAsync();
                }
                else if (task == "2")
                {
                    if (Nowpage != null)
                    {
                        var result = await Nowpage.GetParentCatalogs(session);
                        foreach (Catalog e in result)
                        {
                            Console.WriteLine(e.Author);
                        }
                    }
                }
                else if (task == "3")
                {
                    if (Nowpage != null)
                    {
                        string name;
                        Console.Write("Введите новое имя страницы:");
                        name = Console.ReadLine();
                        if (name != null)
                        {
                            Nowpage.Rename(session, name);
                        }
                    }
                }
                else if (task == "4")
                {
                    if (Nowpage != null)
                    {
                        string username;
                        Console.Write("Введите имя пользователя:");
                        username = Console.ReadLine();
                        if (username != null)
                        {
                            Nowpage.BeginEditBy(session, username);
                        }
                    }
                }
                else if (task == "5")
                {
                    if (Nowpage != null)
                    {
                        string username;
                        Console.Write("Введите имя пользователя:");
                        username = Console.ReadLine();
                        if (username != null)
                        {
                            Nowpage.EndEditBy(session, username);
                        }
                    }
                }
                else if (task == "6")
                {
                    if (Nowcatalog != null)
                    {
                        await Nowcatalog.GetPages(session);
                    }
                }
                else if (task == "7")
                {
                    if (Nowcatalog != null)
                    {
                        await Nowcatalog.GetSubcatalogs(session);
                    }
                }
                else if (task == "8")
                {
                    if (Nowcatalog != null)
                    {
                        await Nowcatalog.GetParentCatalogs(session);
                    }
                }
                else if (task == "9")
                {
                    if (Nowcatalog != null)
                    {
                        string name;
                        Console.Write("Введите новое имя каталога:");
                        name = Console.ReadLine();
                        if (name != null)
                        {
                            Nowcatalog.Rename(session, name);
                        }
                    }
                }
                else if (task == "10")
                {
                    string catalogName, catalogTitle, catalogAuthor;
                    Console.Write("Введите ID каталога:");
                    catalogName = Console.ReadLine();
                    Console.Write("Введите название каталога:");
                    catalogTitle = Console.ReadLine();
                    Console.Write("Введите автора каталога:");
                    catalogAuthor = Console.ReadLine();
                    GraphDB.AddCatalog(session, catalogName, catalogTitle, catalogAuthor);
                }
                else if (task == "11")
                {
                    string pageName, pageTitle, pageAuthor, pageSource, filePath, pageLabels;
                    PageType pageType;
                    Console.Write("Введите ID страницы:");
                    pageName = Console.ReadLine();
                    Console.Write("Введите название страницы:");
                    pageTitle = Console.ReadLine();
                    Console.Write("Введите автора страницы:");
                    pageAuthor = Console.ReadLine();
                    Console.Write("Введите ссылку страницы:");
                    pageSource = Console.ReadLine();
                    Console.Write("Введите путь до файла с данными страницы:");
                    filePath = Console.ReadLine();
                    Console.Write("Введите метки страницы:");
                    pageLabels = Console.ReadLine();
                    Console.Write("Введите тип страницы:");
                    int r = int.Parse(Console.ReadLine());
                    switch (r)
                    {
                        case 1:
                            pageType = PageType.TextDocument;
                            break;
                        case 2:
                            pageType = PageType.ExcelDocument;
                            break;
                        case 3:
                            pageType = PageType.Image;
                            break;
                        case 4:
                            pageType = PageType.WebPage;
                            break;
                        case 5:
                            pageType = PageType.MediaFile;
                            break;
                        case 6:
                            pageType = PageType.OtherFile;
                            break;
                        default:
                            pageType = PageType.OtherFile;
                            break;
                    }
                    AddPage(session, pageName, pageTitle, pageType, pageAuthor, pageSource, filePath, pageLabels);
                }
                else if (task == "12")
                {
                    string relUniqName, relTitle;
                    List<string> startNodeType = new List<string>();
                    List<string> endNodeType = new List<string>();
                    while (true)
                    {
                        string startNode;
                        Console.Write("Введите тип узла из которого может выходить связь(0 для выхода): ");
                        startNode = Console.ReadLine();
                        if (startNode == "0") break;
                        else startNodeType.Add(startNode);
                    }
                    while (true)
                    {
                        string endNode;
                        Console.Write("Введите тип узла к которому может подходить связь(0 для выхода): ");
                        endNode = Console.ReadLine();
                        if (endNode == "0") break;
                        else endNodeType.Add(endNode);
                    }
                    Console.Write("Введите ID типа связи: ");
                    relUniqName = Console.ReadLine();
                    Console.Write("Введите название связи: ");
                    relTitle = Console.ReadLine();
                    CreateRelationshipType(session, relUniqName, relTitle, startNodeType, endNodeType);
                }
                else if (task == "13")
                {
                    string nodeName1, nodeType1, nodeName2, nodeType2, relType;
                    Console.Write("Введите ID первой страницы: ");
                    nodeName1 = Console.ReadLine();
                    Console.Write("Введите тип первой вершины: ");
                    nodeType1 = Console.ReadLine();
                    Console.Write("Введите ID второй страницы: ");
                    nodeName2 = Console.ReadLine();
                    Console.Write("Введите тип второй вершины: ");
                    nodeType2 = Console.ReadLine();
                    Console.Write("Введите тип связи: ");
                    relType = Console.ReadLine();
                    AddRelationship(session, nodeName1, nodeType1, nodeName2, nodeType2, relType);
                }
                else if (task == "14")
                {
                    string catalogName, pageName;
                    Console.Write("Введите ID каталога: ");
                    catalogName = Console.ReadLine();
                    Console.Write("Введите ID страницы: ");
                    pageName = Console.ReadLine();
                    AddPageToCatalog(session, catalogName, pageName);
                }
                else if (task == "15")
                {
                    string catalogName, subCatalogName;
                    Console.Write("Введите ID каталога: ");
                    catalogName = Console.ReadLine();
                    Console.Write("Введите ID подкаталога: ");
                    subCatalogName = Console.ReadLine();
                    AddSubcatalogToCatalog(session, catalogName, subCatalogName);
                }
                else if (task == "16")
                {
                    string pageName, ftpServer;
                    Console.Write("Введите ID страницы: ");
                    pageName = Console.ReadLine();
                    Console.Write("Введите ftp сервер: ");
                    ftpServer = Console.ReadLine();
                    DeletePage(session, pageName, ftpServer);
                }
                else if (task == "17")
                {
                    string catalogName;
                    Console.Write("Введите ID каталога: ");
                    catalogName = Console.ReadLine();
                    DeleteCatalog(session, catalogName);
                }
                else if (task == "18")
                {
                    string fromNodeName, fromNodeType, toNodeName, toNodeType, relType;
                    Console.Write("Введите ID вершины, от которой отходит связь: ");
                    fromNodeName = Console.ReadLine();
                    Console.Write("Введите тип вершины, от которой отходит связь: ");
                    fromNodeType = Console.ReadLine();
                    Console.Write("Введите ID вершины, к которой подходит связь: ");
                    toNodeName = Console.ReadLine();
                    Console.Write("Введите тип вершины, к которой подходит связь: ");
                    toNodeType = Console.ReadLine();
                    Console.Write("Введите тип связи: ");
                    relType = Console.ReadLine();
                    DeleteRelationship(session, fromNodeName, fromNodeType, toNodeName, toNodeType, relType);
                }
                else if (task == "19")
                {
                    string catalogName, pageName;
                    Console.Write("Введите ID каталога: ");
                    catalogName = Console.ReadLine();
                    Console.Write("Введите ID страницы: ");
                    pageName = Console.ReadLine();
                    RemovePageFromCatalog(session, catalogName, pageName);
                }
                else if (task == "20")
                {
                    string parentCatalogName, childCatalogName;
                    Console.Write("Введите ID каталога: ");
                    parentCatalogName = Console.ReadLine();
                    Console.Write("Введите ID подкаталога: ");
                    childCatalogName = Console.ReadLine();
                    RemoveSubcatalogFromCatalog(session, parentCatalogName, childCatalogName);
                }
                else if (task == "21")
                {
                    if (Nowcatalog != null)
                    {
                        string catalogName;
                        Console.Write("Введите ID каталога: ");
                        catalogName = Console.ReadLine();
                        await GetCatalog(session, catalogName);
                    }
                }
                else if (task == "22")
                {
                    if (Nowpage != null)
                    {
                        string pageName;
                        Console.Write("Введите ID страницы: ");
                        pageName = Console.ReadLine();
                        await GetPage(session, pageName);
                    }
                }
                else if (task == "23")
                {
                    string relName;
                    Console.Write("Введите ID связи: ");
                    relName = Console.ReadLine();
                    await GetRelationship(session, relName);
                }
                else if (task == "24")
                {
                    string nodeName, nodeType;
                    Console.Write("Введите ID узла: ");
                    nodeName = Console.ReadLine();
                    Console.Write("Введите тип узла: ");
                    nodeType = Console.ReadLine();
                    await GetExitingNodeRelationships(session, nodeName, nodeType);
                }
                else if (task == "25")
                {
                    string nodeName, nodeType;
                    Console.Write("Введите ID узла: ");
                    nodeName = Console.ReadLine();
                    Console.Write("Введите тип узла: ");
                    nodeType = Console.ReadLine();
                    await GetIncomingNodeRelationships(session, nodeName, nodeType);
                }
                else if (task == "26")
                {
                    string pageTitle;
                    Console.Write("Введите название страницы: ");
                    pageTitle = Console.ReadLine();
                    var result = await GraphDB.GetPagesByTitle(session, pageTitle);
                    if (result.Count > 0)
                        Nowpage = result[0];
                }
                else if (task == "27")
                {
                    await GetAllPages(session);
                }
                else if (task == "28")
                {
                    string catalogTitle;
                    Console.Write("Введите название каталога: ");
                    catalogTitle = Console.ReadLine();
                    var result = await GetCatalogsByTitle(session, catalogTitle);
                    if (result.Count > 0)
                        Nowcatalog = result[0];
                }
                else if (task == "29")
                {
                    await GetAllCatalogs(session);
                }
                else if (task == "30")
                {
                    await GetUsedRelationshipTypes(session);
                }
                else if (task == "31")
                {
                    await GetAllRelationshipTypes(session);
                }
                else if (task == "32")
                {
                    await GetAllLabels(session);
                }
                else if (task == "0")
                {
                    await session.CloseAsync();
                    await driver.CloseAsync();
                }
            }
        }
        public void Dispose()
        {
            _driver.Dispose();
        }
    }

}
