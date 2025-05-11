using System;
using System.Collections.Generic;
using System.Linq;
using CustomerAndServerMaintenanceTracking.DataAccess;
using CustomerAndServerMaintenanceTracking.Models;


namespace CustomerAndServerMaintenanceTracking.Services
{
    public class TagPingAggregator
    {
        private TagRepository tagRepo;
        private CustomerRepository custRepo;
        private PingTaskRepository pingRepo;

        public TagPingAggregator()
        {
            tagRepo = new TagRepository();
            custRepo = new CustomerRepository();
            pingRepo = new PingTaskRepository();
        }
        //public List<TagPingDisplay> GetTagPingDisplays()
        //{
        //    List<TagPingDisplay> results = new List<TagPingDisplay>();

        //    // 1) Retrieve ALL tags
        //    var allTags = tagRepo.GetAllTags();

        //    // 2) Filter out child tags, so only top-level parents or single tags remain
        //    allTags = allTags
        //        .Where(tag => !tagRepo.IsChildTag(tag.Id))
        //        .ToList();

        //    foreach (var parentTag in allTags)
        //    {
        //        // 3) Gather the entire subtree (parent + any descendants)
        //        List<int> allTagIds = tagRepo.GetAllDescendantTagIds(parentTag.Id);

        //        // 4) Retrieve customers that belong to this subtree
        //        List<Customer> customers = custRepo.GetCustomersByTagIds(allTagIds);
        //        int totalCustomers = customers.Count;

        //        int totalAll = 0;      // total tasks
        //        int totalActive = 0;   // tasks with "Active" status
        //        int totalDown = 0;     // tasks with "Down" status

        //        // 5) Summarize ping tasks across the entire family
        //        foreach (var cust in customers)
        //        {
        //            // Gather tasks from parent + all children
        //            List<PingTask> tasksForCust = new List<PingTask>();
        //            foreach (int tagId in allTagIds)
        //            {
        //                tasksForCust.AddRange(
        //                    pingRepo.GetPingTasksByIPAndTag(cust.IPAddress, tagId)
        //                );
        //            }

        //            totalAll += tasksForCust.Count;
        //            totalActive += tasksForCust.Count(t => t.Status == "Active");
        //            totalDown += tasksForCust.Count(t => t.Status == "Down");
        //        }

        //        // **** HERE is where we skip if there are zero tasks ****
        //        // If totalAll == 0, this tag won't appear in the DataGridView.
        //        if (totalAll == 0)
        //            continue;

        //        // 6) Count child tags = allTagIds.Count - 1 (exclude the parent itself)
        //        int childTagCount = allTagIds.Count - 1;
        //        if (childTagCount < 0) childTagCount = 0;

        //        // 7) Build the row
        //        TagPingDisplay row = new TagPingDisplay
        //        {
        //            TagName = parentTag.TagName,
        //            Entity = $"{totalCustomers} customers / {childTagCount} child tag{(childTagCount == 1 ? "" : "s")}",
        //            RtoEntitiesToday = totalDown,
        //            Status = $"{totalActive} active / {totalDown} down"
        //        };

        //        results.Add(row);
        //    }

        //    return results;
        //}

        public List<TagPingDisplay> GetTagPingDisplays()
        {
            List<TagPingDisplay> results = new List<TagPingDisplay>();

            // 1) Get ALL tags from the database
            var allTags = tagRepo.GetAllTags();

            // 2) For each tag, see if it has tasks
            foreach (var tag in allTags)
            {
                // Retrieve only the customers directly assigned to this tag
                List<Customer> customers = custRepo.GetCustomersByTagIds(new List<int> { tag.Id });
                int totalCustomers = customers.Count;

                // We'll count how many tasks exist for this tag
                int totalAll = 0;
                int totalActive = 0;
                int totalDown = 0;

                // 3) For each customer, fetch tasks that belong EXACTLY to (cust.IPAddress, tag.Id)
                foreach (var cust in customers)
                {
                    List<PingTask> tasksForCust = pingRepo.GetPingTasksByIPAndTag(cust.IPAddress, tag.Id);

                    totalAll += tasksForCust.Count;
                    totalActive += tasksForCust.Count(t => t.Status == "Active");
                    totalDown += tasksForCust.Count(t => t.Status == "Down");
                }

                // 4) If you want to hide tags that have zero tasks, skip them:
                if (totalAll == 0)
                    continue;

                // 5) If you still want to display how many child tags exist, you can do:
                //    int childTagCount = tagRepo.GetChildTagCount(tag.Id);
                // or skip it if you don't need child info. We'll just set it to zero for demonstration:
                int childTagCount = tagRepo.GetChildTagCount(tag.Id);

                // 6) Build the row
                TagPingDisplay row = new TagPingDisplay
                {
                    TagName = tag.TagName,
                    Entity = $"{totalCustomers} customers / {childTagCount} child tag{(childTagCount == 1 ? "" : "s")}",
                    RtoEntitiesToday = totalDown,
                    Status = $"{totalActive} active / {totalDown} down"
                };

                results.Add(row);
            }

            return results;
        }




        private string BuildEntityString(int totalCustomers, int childTagCount)
        {
            string customerStr = (totalCustomers == 1)
                ? "1 customer"
                : $"{totalCustomers} customers";

            string childTagStr;
            if (childTagCount == 0)
                childTagStr = "0 child tag";
            else if (childTagCount == 1)
                childTagStr = "1 child tag";
            else
                childTagStr = $"{childTagCount} child tags";

            return $"{customerStr} / {childTagStr}";
        }

    }
}
