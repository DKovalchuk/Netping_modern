﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Policy;
using System.Web;
using System.Web.Http.Routing;
using System.Web.Routing;
using HtmlAgilityPack;
using Microsoft.Web.Mvc;
using NetPing.Controllers;
using NetPing.DAL;
using NetPing.Models;
using NetPing.PriceGeneration;
using NetPing.PriceGeneration.PriceList;

namespace NetPing_modern.PriceGeneration
{
    public class Section : ISection, IDisposable, IBookmark
    {
        private readonly string _sectionId;
        private readonly string _categoryId;
        private readonly SPOnlineRepository _repository = new SPOnlineRepository();

        public Section(string sectionName, string categoryId, string sectionId)
        {
            _sectionId = sectionId;
            _categoryId = categoryId;
            SectionName = sectionName;
        }

        private ICollection<IProduct> _products;

        private ICollection<IProduct> GetProducts(SPOnlineRepository repository, string categoryId, string sectionId)
        {
            var devs = repository.GetDevices(categoryId, sectionId);
            var result = new Collection<IProduct>();
            foreach (var device in devs)
            {
                var product = new Product();
                var price = device.Price;
                if (price != null && price > 0)
                {
                    product.Cost = string.Format(new CultureInfo(1035), "{0:N0} {1}", price, device.GetCurrency);
                }

                product.Title = device.Name.Name;

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(device.Short_description);
                var ulNodes = htmlDoc.DocumentNode.SelectNodes("//ul");
                if (ulNodes != null)
                {
                    foreach (var ulNode in ulNodes)
                    {
                        ulNode.Remove();
                    }
                }
                product.Description = htmlDoc.DocumentNode.InnerText.Replace("&#160;", " ");

                product.ImageFileName = GetImageFileName(device);
                var url =
                    LinkBuilder.BuildUrlFromExpression<Product_itemController>(
                        new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()),
                        RouteTable.Routes, c => c.Index(device.Key));
                Uri uri = HttpContext.Current.Request.Url;
                product.Url = string.Format("{0}://{1}{2}{3}", uri.Scheme, uri.Authority, HttpRuntime.AppDomainAppVirtualPath, url);

                result.Add(product);
            }
            return result;
        }

        public ICollection<IProduct> Products
        {
            get { return _products ?? (_products = GetProducts(_repository, _categoryId, _sectionId)); }
        }

        private string GetImageFileName(Device device)
        {
            var dir = SectionContentDir;
            if (!dir.Exists)
            {
                dir.Create();
            }
            string uriString = device.GetCoverPhoto(true).Url;
            if (string.IsNullOrEmpty(uriString))
            {
                return null;
            }

            Uri address = new Uri(uriString);
            string fileName = dir.FullName + "\\" + address.Segments[address.Segments.Length - 1];
            

            using (WebClient client = new WebClient())
            {
                
                client.DownloadFile(address, fileName);
            }
            return fileName;
        }

        private DirectoryInfo SectionContentDir
        {
            get { return new DirectoryInfo(HttpContext.Current.Server.MapPath("/Content/Price/" + TempDirName)); }
        }

        private string _tempDirName = null;
        private string TempDirName
        {
            get
            {
                if (_tempDirName == null)
                {
                    _tempDirName = Guid.NewGuid().ToString();
                }
                return _tempDirName;
            }
        }

        public string SectionName { get; private set; }
        public void Dispose()
        {
            if (SectionContentDir.Exists)
            {
                try
                {
                    SectionContentDir.Delete(true);
                }
                catch (Exception)
                {
                }
            }
        }

        public string BookmarkName
        {
            get
            {
                return "Section" + _categoryId + _sectionId;
            }
        }
    }
}
