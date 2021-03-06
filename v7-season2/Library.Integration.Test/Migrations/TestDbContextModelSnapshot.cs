﻿// <auto-generated />
using System;
using Library.Integration.Test.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Library.Integration.Test.Migrations
{
    [DbContext(typeof(TestDbContext))]
    partial class TestDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.13");

            modelBuilder.Entity("Library.Components.StateMachines.ThankYouSaga", b =>
                {
                    b.Property<Guid>("CorrelationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("BookId")
                        .HasColumnType("TEXT");

                    b.Property<string>("CurrentState")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("MemberId")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ReservationId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ThankYouStatus")
                        .HasColumnType("INTEGER");

                    b.HasKey("CorrelationId");

                    b.HasIndex("BookId", "MemberId")
                        .IsUnique();

                    b.ToTable("ThankYouSaga");
                });
#pragma warning restore 612, 618
        }
    }
}
