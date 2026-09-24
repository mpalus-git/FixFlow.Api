using System;
using System.Collections.Generic;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FixFlow.Api.Common.Persistence.Migrations
{
    [DbContext(typeof(FixFlowDbContext))]
    [Migration("20260924131336_AddServiceEntries")]
    partial class AddServiceEntries
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("FixFlow.Api.Domain.Auth.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTimeOffset>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<Guid>("FamilyId")
                        .HasColumnType("uuid")
                        .HasColumnName("family_id");

                    b.Property<Guid?>("ReplacedByTokenId")
                        .HasColumnType("uuid")
                        .HasColumnName("replaced_by_token_id");

                    b.Property<DateTimeOffset?>("RevokedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("revoked_at");

                    b.Property<string>("TokenHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("token_hash");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_refresh_tokens");

                    b.HasIndex("FamilyId")
                        .HasDatabaseName("ix_refresh_tokens_family_id");

                    b.HasIndex("TokenHash")
                        .IsUnique()
                        .HasDatabaseName("ix_refresh_tokens_token_hash");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_refresh_tokens_user_id");

                    b.ToTable("refresh_tokens", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Clients.Client", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset?>("ArchivedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("archived_at");

                    b.Property<string>("ContactPerson")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("contact_person");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("email");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("name");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("phone");

                    b.ComplexProperty(typeof(Dictionary<string, object>), "Address", "FixFlow.Api.Domain.Clients.Client.Address#Address", b1 =>
                        {
                            b1.IsRequired();

                            b1.Property<string>("BuildingNumber")
                                .IsRequired()
                                .HasMaxLength(20)
                                .HasColumnType("character varying(20)")
                                .HasColumnName("address_building_number");

                            b1.Property<string>("City")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("character varying(100)")
                                .HasColumnName("address_city");

                            b1.Property<string>("PostalCode")
                                .IsRequired()
                                .HasMaxLength(6)
                                .HasColumnType("character varying(6)")
                                .HasColumnName("address_postal_code");

                            b1.Property<string>("Street")
                                .IsRequired()
                                .HasMaxLength(200)
                                .HasColumnType("character varying(200)")
                                .HasColumnName("address_street");
                        });

                    b.HasKey("Id")
                        .HasName("pk_clients");

                    b.HasIndex("Name")
                        .HasDatabaseName("ix_clients_name");

                    b.ToTable("clients", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Devices.Device", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset?>("ArchivedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("archived_at");

                    b.Property<Guid>("ClientId")
                        .HasColumnType("uuid")
                        .HasColumnName("client_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateOnly>("InstallationDate")
                        .HasColumnType("date")
                        .HasColumnName("installation_date");

                    b.Property<string>("Manufacturer")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("manufacturer");

                    b.Property<string>("Model")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("model");

                    b.Property<string>("SerialNumber")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("serial_number");

                    b.HasKey("Id")
                        .HasName("pk_devices");

                    b.HasIndex("ClientId")
                        .HasDatabaseName("ix_devices_client_id");

                    b.HasIndex("SerialNumber")
                        .IsUnique()
                        .HasDatabaseName("ix_devices_serial_number");

                    b.ToTable("devices", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Parts.Part", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset?>("ArchivedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("archived_at");

                    b.Property<string>("CatalogNumber")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("catalog_number");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("name");

                    b.Property<int>("StockQuantity")
                        .HasColumnType("integer")
                        .HasColumnName("stock_quantity");

                    b.Property<decimal>("UnitPrice")
                        .HasPrecision(12, 2)
                        .HasColumnType("numeric(12,2)")
                        .HasColumnName("unit_price");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_parts");

                    b.HasIndex("CatalogNumber")
                        .IsUnique()
                        .HasDatabaseName("ix_parts_catalog_number");

                    b.ToTable("parts", null, t =>
                        {
                            t.HasCheckConstraint("ck_parts_stock_quantity_non_negative", "stock_quantity >= 0");
                        });
                });

            modelBuilder.Entity("FixFlow.Api.Domain.ServiceEntries.ServiceEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<bool>("IsCorrection")
                        .HasColumnType("boolean")
                        .HasColumnName("is_correction");

                    b.Property<double?>("Latitude")
                        .HasColumnType("double precision")
                        .HasColumnName("latitude");

                    b.Property<double?>("Longitude")
                        .HasColumnType("double precision")
                        .HasColumnName("longitude");

                    b.Property<string>("Note")
                        .IsRequired()
                        .HasMaxLength(4000)
                        .HasColumnType("character varying(4000)")
                        .HasColumnName("note");

                    b.PrimitiveCollection<string[]>("PhotoUrls")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("photo_urls");

                    b.Property<Guid>("TechnicianId")
                        .HasColumnType("uuid")
                        .HasColumnName("technician_id");

                    b.Property<DateTimeOffset?>("WorkFinishedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("work_finished_at");

                    b.Property<Guid>("WorkOrderId")
                        .HasColumnType("uuid")
                        .HasColumnName("work_order_id");

                    b.Property<DateTimeOffset?>("WorkStartedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("work_started_at");

                    b.HasKey("Id")
                        .HasName("pk_service_entries");

                    b.HasIndex("TechnicianId")
                        .HasDatabaseName("ix_service_entries_technician_id");

                    b.HasIndex("WorkOrderId")
                        .HasDatabaseName("ix_service_entries_work_order_id");

                    b.ToTable("service_entries", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Users.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer")
                        .HasColumnName("access_failed_count");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text")
                        .HasColumnName("concurrency_stamp");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("email");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean")
                        .HasColumnName("email_confirmed");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean")
                        .HasColumnName("lockout_enabled");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("lockout_end");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("normalized_email");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("normalized_user_name");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text")
                        .HasColumnName("phone_number");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean")
                        .HasColumnName("phone_number_confirmed");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text")
                        .HasColumnName("security_stamp");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean")
                        .HasColumnName("two_factor_enabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("user_name");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.WorkOrders.WorkOrder", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)")
                        .HasColumnName("description");

                    b.Property<Guid>("DeviceId")
                        .HasColumnType("uuid")
                        .HasColumnName("device_id");

                    b.Property<DateTimeOffset>("DueDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("due_date");

                    b.Property<string>("Priority")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("priority");

                    b.Property<DateTimeOffset?>("StartedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("started_at");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("status");

                    b.Property<Guid?>("TechnicianId")
                        .HasColumnType("uuid")
                        .HasColumnName("technician_id");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id")
                        .HasName("pk_work_orders");

                    b.HasIndex("DeviceId")
                        .HasDatabaseName("ix_work_orders_device_id");

                    b.HasIndex("TechnicianId")
                        .HasDatabaseName("ix_work_orders_technician_id");

                    b.HasIndex(new[] { "TechnicianId" }, "ix_work_orders_technician_id_in_progress")
                        .IsUnique()
                        .HasDatabaseName("ix_work_orders_technician_id_in_progress")
                        .HasFilter("status = 'InProgress'");

                    b.ToTable("work_orders", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text")
                        .HasColumnName("concurrency_stamp");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("name");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("normalized_name");

                    b.HasKey("Id")
                        .HasName("pk_roles");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("roles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text")
                        .HasColumnName("claim_type");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text")
                        .HasColumnName("claim_value");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid")
                        .HasColumnName("role_id");

                    b.HasKey("Id")
                        .HasName("pk_role_claims");

                    b.HasIndex("RoleId")
                        .HasDatabaseName("ix_role_claims_role_id");

                    b.ToTable("role_claims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text")
                        .HasColumnName("claim_type");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text")
                        .HasColumnName("claim_value");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_user_claims");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_user_claims_user_id");

                    b.ToTable("user_claims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text")
                        .HasColumnName("login_provider");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text")
                        .HasColumnName("provider_key");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text")
                        .HasColumnName("provider_display_name");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.HasKey("LoginProvider", "ProviderKey")
                        .HasName("pk_user_logins");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_user_logins_user_id");

                    b.ToTable("user_logins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid")
                        .HasColumnName("role_id");

                    b.HasKey("UserId", "RoleId")
                        .HasName("pk_user_roles");

                    b.HasIndex("RoleId")
                        .HasDatabaseName("ix_user_roles_role_id");

                    b.ToTable("user_roles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text")
                        .HasColumnName("login_provider");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Value")
                        .HasColumnType("text")
                        .HasColumnName("value");

                    b.HasKey("UserId", "LoginProvider", "Name")
                        .HasName("pk_user_tokens");

                    b.ToTable("user_tokens", (string)null);
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Auth.RefreshToken", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_refresh_tokens_users_user_id");
                });

            modelBuilder.Entity("FixFlow.Api.Domain.Devices.Device", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Clients.Client", null)
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_devices_clients_client_id");
                });

            modelBuilder.Entity("FixFlow.Api.Domain.ServiceEntries.ServiceEntry", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("TechnicianId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_service_entries_users_technician_id");

                    b.HasOne("FixFlow.Api.Domain.WorkOrders.WorkOrder", null)
                        .WithMany()
                        .HasForeignKey("WorkOrderId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_service_entries_work_orders_work_order_id");

                    b.OwnsMany("FixFlow.Api.Domain.ServiceEntries.ServiceEntryPart", "Parts", b1 =>
                        {
                            b1.Property<Guid>("ServiceEntryId")
                                .HasColumnType("uuid")
                                .HasColumnName("service_entry_id");

                            b1.Property<Guid>("PartId")
                                .HasColumnType("uuid")
                                .HasColumnName("part_id");

                            b1.Property<int>("Quantity")
                                .HasColumnType("integer")
                                .HasColumnName("quantity");

                            b1.Property<decimal>("UnitPrice")
                                .HasPrecision(12, 2)
                                .HasColumnType("numeric(12,2)")
                                .HasColumnName("unit_price");

                            b1.HasKey("ServiceEntryId", "PartId")
                                .HasName("pk_service_entry_parts");

                            b1.HasIndex("PartId")
                                .HasDatabaseName("ix_service_entry_parts_part_id");

                            b1.ToTable("service_entry_parts", null, t =>
                                {
                                    t.HasCheckConstraint("ck_service_entry_parts_quantity_positive", "quantity > 0");
                                });

                            b1.HasOne("FixFlow.Api.Domain.Parts.Part", null)
                                .WithMany()
                                .HasForeignKey("PartId")
                                .OnDelete(DeleteBehavior.Restrict)
                                .IsRequired()
                                .HasConstraintName("fk_service_entry_parts_parts_part_id");

                            b1.WithOwner()
                                .HasForeignKey("ServiceEntryId")
                                .HasConstraintName("fk_service_entry_parts_service_entries_service_entry_id");
                        });

                    b.Navigation("Parts");
                });

            modelBuilder.Entity("FixFlow.Api.Domain.WorkOrders.WorkOrder", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Devices.Device", null)
                        .WithMany()
                        .HasForeignKey("DeviceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("fk_work_orders_devices_device_id");

                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("TechnicianId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("fk_work_orders_users_technician_id");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_role_claims_roles_role_id");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_claims_users_user_id");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_logins_users_user_id");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_roles_roles_role_id");

                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_roles_users_user_id");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("FixFlow.Api.Domain.Users.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_user_tokens_users_user_id");
                });
#pragma warning restore 612, 618
        }
    }
}
