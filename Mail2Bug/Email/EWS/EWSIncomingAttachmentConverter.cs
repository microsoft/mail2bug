﻿using log4net;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mail2Bug.Email.EWS
{
    /// <summary>
    /// This class processes attachments associated with the incoming email. For file attachments it captures information needed
    /// for inlining of images. It also converts small images to base64 format and removes from them from the attachment list.
    /// Larger images will be processed downstream as image references after the item is created in TFS.
    /// </summary>
    public class EWSIncomingAttachmentConverter
    {
        // TFS drops large base64 inline images: http://johannblais.blogspot.com/2015/08/migrate-tfs-repro-steps-field-to.html
        private const int MaxBase64Length = 22 * 1024; // This a safe guess

        private readonly EmailMessage _message;

        private string _bodyText;
        private List<IIncomingEmailAttachment> _attachments = null;

        public EWSIncomingAttachmentConverter(EmailMessage message)
        {
            _message = message;
        }

        public void ProcessAttachments(bool convertInlineAttachments)
        {
            _bodyText = _message.Body.Text ?? String.Empty;
            _attachments = new List<IIncomingEmailAttachment>();

            bool doInlining = !String.IsNullOrEmpty(_bodyText) && convertInlineAttachments;

            foreach (var ma in _message.Attachments)
            {
                if (doInlining) ma.Load(); // Content property requires load, and for RTF the ContentType property is also not available unless loaded

                if (ma is FileAttachment)
                {
                    _attachments.Add(new EWSIncomingFileAttachment(ma as FileAttachment));
                }
                else if (ma is ItemAttachment)
                {
                    _attachments.Add(new EWSIncomingItemAttachment(ma as ItemAttachment));
                }
                else
                {
                    Logger.ErrorFormat("Skipping attachment because it's not a file attachment ({0})", ma.Name);
                }
            }

            if (doInlining)
            {
                // RTF-email attachments have no ContentId value set. The only way to match them is by position.
                // We want a uniform way to match attachments to content id references, so here we assign
                // the applicable content id to each inline attachment by traversing the HTML for
                // "cid:" references and positionally matching to the attachments.
                // More info: https://blogs.msdn.microsoft.com/webdav_101/2016/06/21/ews-and-inline-attachments/
                if (IsNativeRtfBody(_message))
                    FixupContentIds();

                // Use base64 when possible because these images will display inside Visual Studio from 
                // hosted VSTS (visualstudio.com).
                Base64EncodeImages();
            }
        }

        public string BodyText { get { return _bodyText; } }

        public IEnumerable<IIncomingEmailAttachment> Attachments { get { return _attachments; } }

        private void FixupContentIds()
        {
            const string pattern = @"(\""cid:)(.*)(\"")";

            var inlineAttachments = _attachments.Where(x => x.IsInline);

            int index = 0;

            _bodyText = Regex.Replace(_bodyText, pattern, match =>
            {
                var fileAttachment = inlineAttachments.ElementAtOrDefault(index++) as EWSIncomingFileAttachment;
                if (fileAttachment != null)
                {
                    Logger.DebugFormat("Fixing content id");
                    string contentId = Guid.NewGuid().ToString();
                    fileAttachment.SetContentId(contentId);
                    return $"{match.Groups[1]}{contentId}{match.Groups[3]}";
                }
                return match.Groups[0].ToString();
            });
        }

        private void Base64EncodeImages()
        {
            const string pattern = @"(\<img.* src=\"")(cid:)(.*)(\"".*\>)";

            var inlineAttachments = _attachments.OfType<EWSIncomingFileAttachment>().Where(x => x.IsInline);

            _bodyText = Regex.Replace(_bodyText, pattern, match =>
            {
                string contentId = match.Groups[3].ToString();

                var attachment = inlineAttachments.FirstOrDefault(x => x.ContentId == contentId);

                if (attachment != null)
                {
                    string contentType = attachment.ContentType.ToLower();
                    string base64 = Convert.ToBase64String(attachment.Content);

                    if (base64.Length < MaxBase64Length)
                    {
                        Logger.DebugFormat("Inlining image attachment via base64 encoding");
                        _attachments.Remove(attachment);
                        return $"{match.Groups[1]}data:{contentType};base64,{base64}{match.Groups[4]}";
                    }
                }
                return match.Groups[0].ToString();
            });
        }

        private static bool IsNativeRtfBody(EmailMessage message)
        {
            int nativeBodyResult = 0;

            if (message.TryGetProperty(EWSExtendedProperty.PidTagNativeBody, out nativeBodyResult))
            {
                return nativeBodyResult == EWSExtendedProperty.PidTagNativeBodyRTFCompressed;
            }

            // Optionally, in case native body property is not available, we could attempt to infer from ContentId in attachments.
            return false;
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(EWSIncomingAttachmentConverter));
    }
}