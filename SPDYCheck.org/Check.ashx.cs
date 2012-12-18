﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

using Zoompf.SPDYAnalysis;
using Zoompf.General;
using Zoompf.General.Collections;

namespace SPDYCheck.org
{
    /// <summary>
    /// Summary description for Check
    /// </summary>
    public class Check : IHttpHandler
    {
        
        public static OCache<SPDYResult> cache = new OCache<SPDYResult>();

        public void ProcessRequest(HttpContext context)
        {
            
            context.Response.ContentType = "text/plain";

            string host = Normalize(context.Request.QueryString["host"]);
            host = ValidateRegex(host.ToLower(), new Regex(@"^[a-zA-Z0-9\-\.]+$"));

            JObject resp = new JObject();

            if (String.IsNullOrEmpty(host))
            {
                resp["bad"] = true;

            }
            else
            {

                bool fromCache = true;
                SPDYResult result = cache.Get(host);
                if (result == null)
                {
                    fromCache = false;
                    result = SPDYChecker.Test(host, 443, 8000);
                    cache.Add(host, result);
                }

                TestLog.Log(fromCache, result, context.Request.UserHostAddress);

                ////Hurray! Everything worked!

                JArray a;
                resp["Host"] = result.Hostname;
                resp["bad"] = false;
                resp["ConnectivityHTTP"] = result.ConnectivityHTTP;
                resp["HTTPServerHeader"] = result.HTTPServerHeader;

                resp["ConnectivitySSL"] = result.ConnectivitySSL;
                resp["SpeaksSSL"] = result.SpeaksSSL;
                resp["SupportSSLHTTPFallback"] = result.SupportSSLHTTPFallback;

                resp["RedirectsToSSL"] = result.RedirectsToSSL;

                resp["SSLCertificateValid"] = result.SSLCertificateValid;

                if (!result.SSLCertificateValid)
                {
                    a = new JArray();
                    foreach (SSLCertError s in result.CertErrors)
                    {
                        a.Add(s.ToString());
                    }
                    resp["CertErrors"] = a;
                }

                resp["SSLServerHeader"] = result.SSLServerHeader;
                resp["HasNPNExtension"] = result.HasNPNExtension;
                resp["SupportsSPDY"] = result.SupportsSPDY;
                if (result.SupportsSPDY)
                {


                    a = new JArray();
                    foreach (string s in result.SPDYProtocols)
                    {
                        a.Add(s);
                    }
                    resp["SPDYProtocols"] = a;
                }
            }

            context.Response.Write(resp.ToString());
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


        //=========== Helper Methods
        public static String Normalize(object o)
        {
            return (o != null) ? o.ToString().Trim() : String.Empty;
        }

        public static String ValidateRegex(String s, Regex regex)
        {
            s = Normalize(s);
            if (!regex.IsMatch(s))
            {
                return String.Empty;
            }
            return s;
        }


    }
}