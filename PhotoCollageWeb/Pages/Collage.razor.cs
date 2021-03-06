﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using PhotoCollage.Common;
using PhotoCollage.Common.Data;
using PhotoCollageWeb.Models;

namespace PhotoCollageWeb.Pages
{
    public partial class Collage : IDisposable
    {
        private readonly Queue<PhotoData> images = new Queue<PhotoData>();
        private int count = 0;
        private IPhotoRepository photoRepository;
        private Timer timer;

        [Inject] protected IOptions<CollageSettings> Options { get; set; }
        protected CollageSettings Settings => this.Options.Value;

        /// <summary>
        /// During prerender, this component is rendered without calling OnAfterRender and then immediately disposed
        /// this mean timer will be null so we have to check for null or use the Null-conditional operator ?
        /// </summary>
        public void Dispose() => this.timer?.Dispose();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                this.InitializeCollage();
                this.ShowNextPhoto();
                this.StateHasChanged();
                this.TryStartTimer();
            }

            base.OnAfterRender(firstRender);
        }

        private async void OnTimerInterval(object sender, ElapsedEventArgs e)
        {
            this.TryStopTimer();
            this.ShowNextPhoto();
            this.TryStartTimer();
            await this.InvokeAsync(() => this.StateHasChanged());
        }

        private void InitializeCollage()
        {
            this.photoRepository = new PhotoRepositoryFactory(this.Settings).Make();
            var speed = (int)this.Settings.Speed;
            this.timer ??= new Timer
            {
                Interval = TimeSpan.FromSeconds(speed).TotalMilliseconds,
                AutoReset = true
            };
            this.timer.Elapsed += this.OnTimerInterval;
        }

        private void ShowNextPhoto()
        {
            if (this.photoRepository.HasPhotos)
            {
                var path = this.photoRepository.GetNextPhotoFilePath();
                var extension = System.IO.Path.GetExtension(path);
                var bytes = System.IO.File.ReadAllBytes(path);
                var image = new PhotoData(++this.count, this.Settings)
                {
                    Extension = extension,
                    Data = Convert.ToBase64String(bytes)
                };
                this.images.Enqueue(image);

                if (this.images.Count > (this.Settings.NumberOfPhotos + 1))
                {
                    _ = this.images.Dequeue();
                }

                if (this.images.Count > this.Settings.NumberOfPhotos)
                {
                    var faded = this.images.Peek();
                    faded.IsRemoved = true;
                }
            }
        }

        private void TryStartTimer()
        {
            if (this.timer != null)
            {
                this.timer.Start();
            }
        }

        private void TryStopTimer()
        {
            if (this.timer != null)
            {
                this.timer.Stop();
            }
        }
    }
}
