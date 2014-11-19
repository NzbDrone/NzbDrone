﻿using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Notifications.Email
{
    public class Email : NotificationBase<EmailSettings>
    {
        private readonly IEmailService _emailService;

        public Email(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public override string Link
        {
            get { return null; }
        }

        public override void OnGrab(string message)
        {
            const string subject = "Sonarr [TV] - Grabbed";
            var body = String.Format("{0} sent to queue.", message);

            _emailService.SendEmail(Settings, subject, body);
        }

        public override void OnDownload(DownloadMessage message)
        {
            const string subject = "Sonarr [TV] - Downloaded";
            var body = String.Format("{0} Downloaded and sorted.", message.Message);

            _emailService.SendEmail(Settings, subject, body);
        }

        public override void AfterRename(Series series)
        {
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_emailService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
