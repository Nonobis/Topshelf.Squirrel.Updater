﻿namespace Topshelf.Squirrel.Windows.Interfaces
{
	public interface ISelfUpdatableService
	{
        /// <summary>
        /// Starts self updater
        /// </summary>
        void Start();

        /// <summary>
        /// Stops self updater
        /// </summary>
        void Stop();
	}
}