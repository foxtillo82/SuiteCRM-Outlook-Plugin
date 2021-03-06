﻿namespace SuiteCRMAddIn.BusinessLogic
{
    using Microsoft.Office.Interop.Outlook;
    using SuiteCRMClient;
    using SuiteCRMClient.Email;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class EmailArchiveAction : DaemonAction
    {
        private readonly IEnumerable<MailItem> items;

        private readonly IEnumerable<CrmEntity> entities;

        private readonly string type;

        public EmailArchiveAction(IEnumerable<MailItem> items, IEnumerable<CrmEntity> entities, string type)
        {
            this.items = items;
            this.entities = entities;
            this.type = type;
        }

        public string Description
        {
            get
            {
                return $"Archiving {items.Count()} items"; ;
            }
        }

        public void Perform()
        {
            var archiver = new EmailArchiving($"EB-{Globals.ThisAddIn.SelectedEmailCount}", Globals.ThisAddIn.Log);
            this.ReportOnEmailArchiveSuccess(
                items.Select(mailItem =>
                        archiver.ArchiveEmailWithEntityRelationships(mailItem, this.entities, this.type))
                    .ToList());
        }

        private bool ReportOnEmailArchiveSuccess(List<ArchiveResult> emailArchiveResults)
        {
            var successCount = emailArchiveResults.Count(r => r.IsSuccess);
            var failCount = emailArchiveResults.Count - successCount;
            var fullSuccess = failCount == 0;
            if (fullSuccess)
            {
                if (Globals.ThisAddIn.Settings.ShowConfirmationMessageArchive)
                {
                    MessageBox.Show(
                        $"{successCount} email(s) have been successfully archived",
                        "Archived successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            }
            else
            {
                var message = successCount == 0
                    ? $"Failed to archive {failCount} email(s)"
                    : $"{successCount} emails(s) were successfully archived.";

                var first11Problems = emailArchiveResults.SelectMany(r => r.Problems).Take(11).ToList();
                if (first11Problems.Any())
                {
                    message =
                        message +
                        "\n\nThere were some failures:\n" +
                        string.Join("\n", first11Problems.Take(10)) +
                        (first11Problems.Count > 10 ? "\n[and more]" : "");
                }

                MessageBox.Show(message, "Archive failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
