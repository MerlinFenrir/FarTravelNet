﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.IO;

namespace FarTravelNet.Api.Controllers
{
    /// <summary>
    /// 文件上传控制器
    /// </summary>
    public class UploadController : BaseController
    {
        #region 外部接口

        /// <summary>
        /// 文件上传
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [SwaggerFileUpload(true)]
        public ActionResult<ApiResult> UpLoad()
        {
            if (Request.Form.Files == null || Request.Form.Files[0] == null)
                return Error("请上传文件");
            var file = Request.Form.Files[0];
            var upFileName = ContentDispositionHeaderValue
                   .Parse(file.ContentDisposition)
                   .FileName
                   .Trim('"');
            //大小，格式校验....
            var fileName = Guid.NewGuid() + Path.GetExtension(upFileName);

            string fileExtensions = upFileName.Split('.').Last();

            var saveDir = $@".\wwwroot\uploads\{fileExtensions}\";
            var savePath = saveDir + fileName;
            var previewPath = $"/uploads/{fileExtensions}/{fileName}";

            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            using (FileStream fs = System.IO.File.Create(savePath))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
            return Success("上传成功", new { path = previewPath });
        }
        #endregion
    }
}