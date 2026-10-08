using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";
        public const string SalesExecutiveRole = "Sales Executive";

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created and up to date
            if (context.Database.ProviderName != null && context.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            // 1. Seed Roles
            string[] roles = [AdminRole, ManagerRole, SalesExecutiveRole];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.com",
                    Email = "admin@acxiomcrm.com",
                    FullName = "System Administrator",
                    Department = "Executive IT",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, AdminRole);
                }
            }

            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.com",
                    Email = "manager@acxiomcrm.com",
                    FullName = "Sarah Jenkins",
                    Department = "Sales Management",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, ManagerRole);
                }
            }

            var salesUser1 = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
            if (salesUser1 == null)
            {
                salesUser1 = new ApplicationUser
                {
                    UserName = "sales@acxiomcrm.com",
                    Email = "sales@acxiomcrm.com",
                    FullName = "David Miller",
                    Department = "Direct Sales",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                };
                var result = await userManager.CreateAsync(salesUser1, "Sales@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser1, SalesExecutiveRole);
                }
            }

            var salesUser2 = await userManager.FindByEmailAsync("sales2@acxiomcrm.com");
            if (salesUser2 == null)
            {
                salesUser2 = new ApplicationUser
                {
                    UserName = "sales2@acxiomcrm.com",
                    Email = "sales2@acxiomcrm.com",
                    FullName = "Emma Watson",
                    Department = "Inside Sales",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                var result = await userManager.CreateAsync(salesUser2, "Sales@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser2, SalesExecutiveRole);
                }
            }

            // 3. Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new()
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Robert Taylor",
                        CompanyName = "Apex Global Logistics",
                        Email = "rtaylor@apexlogistics.com",
                        Phone = "+1-555-0101",
                        Address = "100 Gateway Blvd, Suite 400",
                        City = "Chicago",
                        State = "IL",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        CreatedBy = "admin@acxiomcrm.com",
                        AssignedToId = salesUser1.Id
                    },
                    new()
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Dr. Elena Vance",
                        CompanyName = "BioHealth Dynamics",
                        Email = "evance@biohealthdyn.com",
                        Phone = "+1-555-0102",
                        Address = "750 Medical Plaza Drive",
                        City = "Boston",
                        State = "MA",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = "admin@acxiomcrm.com",
                        AssignedToId = salesUser1.Id
                    },
                    new()
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Marcus Vance",
                        CompanyName = "CloudScale Technologies",
                        Email = "mvance@cloudscale.io",
                        Phone = "+1-555-0103",
                        Address = "404 Innovation Way",
                        City = "Austin",
                        State = "TX",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = "manager@acxiomcrm.com",
                        AssignedToId = salesUser2.Id
                    },
                    new()
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Victoria Hughes",
                        CompanyName = "Delta Finance Group",
                        Email = "vhughes@deltafinance.com",
                        Phone = "+1-555-0104",
                        Address = "1200 Wall St, 28th Fl",
                        City = "New York",
                        State = "NY",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedBy = "manager@acxiomcrm.com",
                        AssignedToId = salesUser2.Id
                    },
                    new()
                    {
                        CustomerCode = "CUST-1005",
                        CustomerName = "Alexander Stone",
                        CompanyName = "Echo Maritime Services",
                        Email = "astone@echomaritime.com",
                        Phone = "+1-555-0105",
                        Address = "21 Harbourfront Ave",
                        City = "Seattle",
                        State = "WA",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedBy = "manager@acxiomcrm.com",
                        AssignedToId = managerUser.Id
                    },
                    new()
                    {
                        CustomerCode = "CUST-1006",
                        CustomerName = "Sophia Ramirez",
                        CompanyName = "Fusion Media Corp",
                        Email = "sramirez@fusionmedia.net",
                        Phone = "+1-555-0106",
                        Address = "88 Sunset Blvd",
                        City = "Los Angeles",
                        State = "CA",
                        Status = "Inactive",
                        CreatedDate = DateTime.UtcNow.AddMonths(-4),
                        CreatedBy = "admin@acxiomcrm.com",
                        AssignedToId = salesUser1.Id
                    }
                };
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            var custApex = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1001");
            var custBio = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1002");
            var custCloud = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1003");
            var custDelta = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1004");
            var custEcho = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-1005");

            // 4. Seed Leads
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new()
                    {
                        LeadCode = "LEAD-2001",
                        LeadName = "Jonathan Drake",
                        CompanyName = "Nexa Robotics Inc",
                        Email = "jdrake@nexarobotics.com",
                        Phone = "+1-555-0201",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 45000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Notes = "Interested in fleet control integrations."
                    },
                    new()
                    {
                        LeadCode = "LEAD-2002",
                        LeadName = "Rebecca Chen",
                        CompanyName = "Horizon Retailers",
                        Email = "rchen@horizonretail.com",
                        Phone = "+1-555-0202",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 28000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Notes = "Introductory discovery call conducted."
                    },
                    new()
                    {
                        LeadCode = "LEAD-2003",
                        LeadName = "Gavin Belson",
                        CompanyName = "Vertex Cyber Defense",
                        Email = "gbelson@vertexcyber.com",
                        Phone = "+1-555-0203",
                        Source = "Event",
                        Status = "New",
                        Priority = "Critical",
                        ExpectedValue = 65000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        CreatedBy = "sales2@acxiomcrm.com",
                        AssignedToId = salesUser2.Id,
                        Notes = "Met at TechSummit 2026."
                    },
                    new()
                    {
                        LeadCode = "LEAD-2004",
                        LeadName = "Thomas Anderson",
                        CompanyName = "Metacortex Logistics",
                        Email = "tanderson@metacortex.net",
                        Phone = "+1-555-0204",
                        Source = "Cold Call",
                        Status = "Converted",
                        Priority = "High",
                        ExpectedValue = 85000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-40),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        CustomerId = custApex?.CustomerId,
                        ConvertedDate = DateTime.UtcNow.AddDays(-10),
                        Notes = "Successfully converted to Apex Logistics account."
                    },
                    new()
                    {
                        LeadCode = "LEAD-2005",
                        LeadName = "Rachel Green",
                        CompanyName = "Alpha Health Networks",
                        Email = "rgreen@alphahealth.org",
                        Phone = "+1-555-0205",
                        Source = "Partner",
                        Status = "Lost",
                        Priority = "Low",
                        ExpectedValue = 15000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-35),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Notes = "Opted for internal open-source tool."
                    },
                    new()
                    {
                        LeadCode = "LEAD-2006",
                        LeadName = "Lucas Meyer",
                        CompanyName = "Sterling Legal Partners",
                        Email = "lmeyer@sterlinglegal.com",
                        Phone = "+1-555-0206",
                        Source = "Social Media",
                        Status = "Qualified",
                        Priority = "Medium",
                        ExpectedValue = 35000.00m,
                        CreatedDate = DateTime.UtcNow.AddDays(-7),
                        CreatedBy = "sales2@acxiomcrm.com",
                        AssignedToId = salesUser2.Id,
                        Notes = "Demo scheduled for next Tuesday."
                    }
                };
                await context.Leads.AddRangeAsync(leads);
                await context.SaveChangesAsync();
            }

            var leadDrake = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-2001");
            var leadChen = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-2002");

            // 5. Seed Opportunities
            if (!await context.Opportunities.AnyAsync() && custApex != null && custBio != null && custCloud != null && custDelta != null)
            {
                var opportunities = new List<Opportunity>
                {
                    new()
                    {
                        OpportunityCode = "OPP-3001",
                        OpportunityName = "Enterprise Cloud Migration",
                        CustomerId = custCloud.CustomerId,
                        Amount = 120000.00m,
                        Stage = "Proposal",
                        Probability = 60,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        CreatedBy = "sales2@acxiomcrm.com",
                        AssignedToId = salesUser2.Id,
                        Description = "Multi-region cloud infrastructure transformation."
                    },
                    new()
                    {
                        OpportunityCode = "OPP-3002",
                        OpportunityName = "Fleet Telematics & Tracking Suite",
                        CustomerId = custApex.CustomerId,
                        LeadId = leadDrake?.LeadId,
                        Amount = 85000.00m,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(14),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Description = "Real-time dispatch telemetry for 450 vehicles."
                    },
                    new()
                    {
                        OpportunityCode = "OPP-3003",
                        OpportunityName = "Medical Records Automation Engine",
                        CustomerId = custBio.CustomerId,
                        Amount = 55000.00m,
                        Stage = "Qualification",
                        Probability = 30,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Description = "HIPAA-compliant document processing pipeline."
                    },
                    new()
                    {
                        OpportunityCode = "OPP-3004",
                        OpportunityName = "Global Compliance Suite",
                        CustomerId = custDelta.CustomerId,
                        Amount = 95000.00m,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-5),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddDays(-45),
                        CreatedBy = "sales2@acxiomcrm.com",
                        AssignedToId = salesUser2.Id,
                        Description = "Closed contract for enterprise auditing solution."
                    },
                    new()
                    {
                        OpportunityCode = "OPP-3005",
                        OpportunityName = "Legacy Infrastructure Overhaul",
                        CustomerId = custApex.CustomerId,
                        Amount = 30000.00m,
                        Stage = "Lost",
                        Probability = 0,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-15),
                        Status = "Lost",
                        CreatedDate = DateTime.UtcNow.AddDays(-50),
                        CreatedBy = "sales@acxiomcrm.com",
                        AssignedToId = salesUser1.Id,
                        Description = "Client postponed capital expense budget."
                    },
                    new()
                    {
                        OpportunityCode = "OPP-3006",
                        OpportunityName = "Maritime Automated Dispatch",
                        CustomerId = custEcho?.CustomerId ?? custApex.CustomerId,
                        Amount = 140000.00m,
                        Stage = "Proposal",
                        Probability = 50,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(35),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        CreatedBy = "manager@acxiomcrm.com",
                        AssignedToId = managerUser.Id,
                        Description = "Harbour vessel dispatch workflow engine."
                    }
                };
                await context.Opportunities.AddRangeAsync(opportunities);
                await context.SaveChangesAsync();
            }

            // 6. Seed Follow-ups
            if (!await context.FollowUps.AnyAsync() && custApex != null)
            {
                var followUps = new List<FollowUp>
                {
                    new()
                    {
                        CustomerId = custApex.CustomerId,
                        LeadId = leadDrake?.LeadId,
                        FollowUpDate = DateTime.Today.AddDays(1).AddHours(10),
                        FollowUpType = "Call",
                        Remarks = "Discuss final quote adjustments with procurement director.",
                        Status = "Planned",
                        AssignedToId = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        CreatedBy = "sales@acxiomcrm.com"
                    },
                    new()
                    {
                        CustomerId = custBio?.CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(3).AddHours(14),
                        FollowUpType = "Demo",
                        Remarks = "Live security architecture demo with CTO and compliance team.",
                        Status = "Planned",
                        AssignedToId = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        CreatedBy = "sales@acxiomcrm.com"
                    },
                    new()
                    {
                        CustomerId = custCloud?.CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(2).AddHours(11),
                        FollowUpType = "Meeting",
                        Remarks = "Present SLA agreement and pricing tiers.",
                        Status = "Planned",
                        AssignedToId = salesUser2.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        CreatedBy = "sales2@acxiomcrm.com"
                    },
                    new()
                    {
                        LeadId = leadChen?.LeadId,
                        FollowUpDate = DateTime.Today.AddDays(-2).AddHours(15),
                        FollowUpType = "Call",
                        Remarks = "Check in on budget approval response from executive board.",
                        Status = "Missed",
                        AssignedToId = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        CreatedBy = "sales@acxiomcrm.com"
                    },
                    new()
                    {
                        CustomerId = custDelta?.CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(-6).AddHours(16),
                        FollowUpType = "Email",
                        Remarks = "Send countersigned master services agreement.",
                        Status = "Completed",
                        AssignedToId = salesUser2.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        CreatedBy = "sales2@acxiomcrm.com",
                        CompletedDate = DateTime.UtcNow.AddDays(-6),
                        CompletionNotes = "Agreement sent and acknowledged by Legal."
                    }
                };
                await context.FollowUps.AddRangeAsync(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!await context.Activities.AnyAsync() && custApex != null)
            {
                var activities = new List<Activity>
                {
                    new()
                    {
                        ActivityType = "Meeting",
                        Subject = "Executive Project Kickoff",
                        Description = "Alignment session with VP of Operations regarding rollout milestones.",
                        ActivityDate = DateTime.UtcNow.AddDays(-8),
                        CustomerId = custApex.CustomerId,
                        AssignedToId = salesUser1.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        CreatedBy = "sales@acxiomcrm.com"
                    },
                    new()
                    {
                        ActivityType = "Call",
                        Subject = "Initial Discovery Phone Call",
                        Description = "Reviewed high-level pain points and current software stack.",
                        ActivityDate = DateTime.UtcNow.AddDays(-14),
                        LeadId = leadDrake?.LeadId,
                        AssignedToId = salesUser1.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-14),
                        CreatedBy = "sales@acxiomcrm.com"
                    },
                    new()
                    {
                        ActivityType = "Email",
                        Subject = "Proposal V2 Sent",
                        Description = "Transmitted revised commercial quote with 3-year discounted term.",
                        ActivityDate = DateTime.UtcNow.AddDays(-4),
                        CustomerId = custCloud?.CustomerId,
                        AssignedToId = salesUser2.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-4),
                        CreatedBy = "sales2@acxiomcrm.com"
                    },
                    new()
                    {
                        ActivityType = "Task",
                        Subject = "Prepare Security Whitepaper Packet",
                        Description = "Gather SOC2 Type II audit report and cloud infrastructure schematics.",
                        ActivityDate = DateTime.UtcNow.AddDays(1),
                        CustomerId = custBio?.CustomerId,
                        AssignedToId = salesUser1.Id,
                        Status = "Pending",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        CreatedBy = "sales@acxiomcrm.com"
                    }
                };
                await context.Activities.AddRangeAsync(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Initial Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                var auditLogs = new List<AuditLog>
                {
                    new()
                    {
                        UserId = adminUser.Id,
                        UserEmail = adminUser.Email,
                        Action = "Login",
                        EntityName = "Auth",
                        RecordId = adminUser.Id,
                        Details = "System administrator initialized platform environment.",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new()
                    {
                        UserId = adminUser.Id,
                        UserEmail = adminUser.Email,
                        Action = "Role Change",
                        EntityName = "User",
                        RecordId = managerUser.Id,
                        Details = "Assigned Manager role to Sarah Jenkins.",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddDays(-9)
                    },
                    new()
                    {
                        UserId = salesUser1.Id,
                        UserEmail = salesUser1.Email,
                        Action = "Lead Conversion",
                        EntityName = "Lead",
                        RecordId = "LEAD-2004",
                        Details = "Converted lead Metacortex Logistics to customer Apex Global Logistics.",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new()
                    {
                        UserId = salesUser2.Id,
                        UserEmail = salesUser2.Email,
                        Action = "Update",
                        EntityName = "Opportunity",
                        RecordId = "OPP-3004",
                        OldValue = "Stage: Negotiation, Probability: 80",
                        NewValue = "Stage: Won, Probability: 100",
                        Details = "Opportunity marked as Won.",
                        IpAddress = "127.0.0.1",
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    }
                };
                await context.AuditLogs.AddRangeAsync(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
