﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CrowdFundingShop.UI.Controllers.PC
{
    public class HomeController : PCBaseController
    {
        //
        // GET: /Home/

        public ActionResult IndexPage()
        {
            return View();
        }

    }
}