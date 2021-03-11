﻿// <auto-generated />
using System;
using MassTransit.EntityFrameworkCoreIntegration.JobService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LongRun.Worker.Migrations
{
    [DbContext(typeof(JobServiceSagaDbContext))]
    [Migration("20210311035208_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.13")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("MassTransit.JobService.Components.StateMachines.JobAttemptSaga", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<int>("CurrentState")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("Faulted")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("InstanceAddress")
                        .HasColumnType("text");

                    b.Property<Guid>("JobId")
                        .HasColumnType("uuid");

                    b.Property<int>("RetryAttempt")
                        .HasColumnType("integer");

                    b.Property<string>("ServiceAddress")
                        .HasColumnType("text");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("StatusCheckTokenId")
                        .HasColumnType("uuid");

                    b.HasKey("CorrelationId");

                    b.HasIndex("JobId", "RetryAttempt")
                        .IsUnique();

                    b.ToTable("JobAttemptSaga");
                });

            modelBuilder.Entity("MassTransit.JobService.Components.StateMachines.JobSaga", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AttemptId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("Completed")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("CurrentState")
                        .HasColumnType("integer");

                    b.Property<TimeSpan?>("Duration")
                        .HasColumnType("interval");

                    b.Property<DateTime?>("Faulted")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Job")
                        .HasColumnType("text");

                    b.Property<Guid?>("JobRetryDelayToken")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("JobSlotWaitToken")
                        .HasColumnType("uuid");

                    b.Property<TimeSpan?>("JobTimeout")
                        .HasColumnType("interval");

                    b.Property<Guid>("JobTypeId")
                        .HasColumnType("uuid");

                    b.Property<string>("Reason")
                        .HasColumnType("text");

                    b.Property<int>("RetryAttempt")
                        .HasColumnType("integer");

                    b.Property<string>("ServiceAddress")
                        .HasColumnType("text");

                    b.Property<DateTime?>("Started")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("Submitted")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("CorrelationId");

                    b.ToTable("JobSaga");
                });

            modelBuilder.Entity("MassTransit.JobService.Components.StateMachines.JobTypeSaga", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("uuid");

                    b.Property<int>("ActiveJobCount")
                        .HasColumnType("integer");

                    b.Property<string>("ActiveJobs")
                        .HasColumnType("text");

                    b.Property<int>("ConcurrentJobLimit")
                        .HasColumnType("integer");

                    b.Property<int>("CurrentState")
                        .HasColumnType("integer");

                    b.Property<int?>("OverrideJobLimit")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("OverrideLimitExpiration")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("CorrelationId");

                    b.ToTable("JobTypeSaga");
                });
#pragma warning restore 612, 618
        }
    }
}
