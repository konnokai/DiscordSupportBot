﻿// <auto-generated />
using System;
using Discord_Support_Bot.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Discord_Support_Bot.Migrations
{
    [DbContext(typeof(SupportContext))]
    [Migration("20220813051631_RenameLottery")]
    partial class RenameLottery
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.7");

            modelBuilder.Entity("Discord_Support_Bot.SQLite.Table.Lottery", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AwardContext")
                        .HasColumnType("TEXT");

                    b.Property<string>("Context")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("Guid")
                        .HasColumnType("TEXT");

                    b.Property<int>("MaxAward")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ParticipantList")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Lottery");
                });

            modelBuilder.Entity("Discord_Support_Bot.SQLite.Table.NCChannelCOD", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("CODId")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Platform")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("NCChannelCOD");
                });

            modelBuilder.Entity("Discord_Support_Bot.SQLite.Table.TrustedGuild", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("TrustedGuild");
                });

            modelBuilder.Entity("Discord_Support_Bot.SQLite.Table.UpdateGuildInfo", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelMemberId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelNitroId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LastTwitterProfileURL")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("NoticeChangeAvatarChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TwitterId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("UpdateGuildInfo");
                });
#pragma warning restore 612, 618
        }
    }
}
