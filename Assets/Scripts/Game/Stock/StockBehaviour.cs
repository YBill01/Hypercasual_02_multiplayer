using GameName.Data;
using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

public class StockBehaviour : IDisposable
{
	private List<StockTransfer> _stockTransfers;
	private List<StockTransfer> _stockTransfersExecute;

	private SharedViewData _viewData;
	private StockEvents _stockEvents;
	private CollectingEvents _collectingEvents;

	private CompositeDisposable _compositeDisposable;

	public StockBehaviour(
		SharedViewData viewData,
		StockEvents stockEvents,
		CollectingEvents collectingEvents)
	{
		_viewData = viewData;
		_stockEvents = stockEvents;
		_collectingEvents = collectingEvents;

		_compositeDisposable = new CompositeDisposable();

		_stockTransfers = new List<StockTransfer>();
		_stockTransfersExecute = new List<StockTransfer>();

		_stockEvents.OnLink
			.Subscribe(x => OnLink(x))
			.AddTo(_compositeDisposable);

		_stockEvents.OnUnlink
			.Subscribe(x => OnUnlink(x))
			.AddTo(_compositeDisposable);

		_stockEvents.OnTransfer
			.Subscribe(x => OnTransfer(x))
			.AddTo(_compositeDisposable);
	}

	private void OnLink(StockLink link)
	{
		if (link.handler.TryStockLink(link))
		{
			//Debug.Log($"linked");
		}
		else
		{
			Debug.Log($"linked error");
		}
	}
	private void OnUnlink(StockLink link)
	{
		if (link.handler.TryStockUnlink(link))
		{
			//Debug.Log($"unlinked");
		}
		else
		{
			Debug.Log($"unlinked error");
		}
	}

	private void OnTransfer(StockTransfer transfer)
	{
		_stockTransfers.Add(transfer);
	}

	public void Execute()
	{
		_stockTransfersExecute.Clear();

		for (int i = 0; i < _stockTransfers.Count; i++)
		{
			if (_stockTransfers[i].stockIn.Has(_stockTransfers[i].item) && _stockTransfers[i].stockOut.HasEmpty(_stockTransfers[i].item))
			{
				if (_stockTransfers[i].stockIn.TryTake(_stockTransfers[i].item, out StockCollectingInfo takeCollectingInfo))
				{
					float duration = _stockTransfers[i].stockIn.TransferDuration + _stockTransfers[i].stockOut.TransferDuration;

					if (_stockTransfers[i].stockOut.TryAdd(_stockTransfers[i].item, out StockCollectingInfo addCollectingInfo, duration))
					{
						if (duration > 0.0f)
						{
							_collectingEvents.OnTransfer.OnNext(new StockCollectingTransfer
							{
								originInfo = takeCollectingInfo,
								targetInfo = addCollectingInfo,

								prefab = _viewData.GetItemViewData(_stockTransfers[i].item.type).prefab,

								duration = duration
							});
						}

						_stockTransfersExecute.Add(_stockTransfers[i]);
					}
				}
			}
		}

		if (_stockTransfersExecute.Count > 0)
		{
			_stockEvents.OnTransferExecute.OnNext(_stockTransfersExecute);
		}

		_stockTransfers.Clear();
	}

	public void Dispose()
	{
		_compositeDisposable.Dispose();
	}
}