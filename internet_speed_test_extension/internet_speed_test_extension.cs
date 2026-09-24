// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace internet_speed_test_extension;

[Guid("80081f57-4220-4ebb-bbff-3364a57d0625")]
public sealed partial class internet_speed_test_extension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly internet_speed_test_extensionCommandsProvider _provider = new();

    public internet_speed_test_extension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}
