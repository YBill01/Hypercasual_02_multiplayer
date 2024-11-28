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
			StockTransfer transfer = _stockTransfers[0];

			if (ExecuteOnce())
			{
				_stockTransfersExecute.Add(transfer);
			}
		}

		if (_stockTransfersExecute.Count > 0)
		{
			_stockEvents.OnTransferExecute.OnNext(_stockTransfersExecute);
		}
	}

	public bool ExecuteOnce(bool forced = false)
	{
		if (_stockTransfers.Count == 0)
		{
			return false;
		}

		StockTransfer transfer = _stockTransfers[0];
		_stockTransfers.RemoveAt(0);

		if (transfer.stockIn.Has(transfer.item) && transfer.stockOut.HasEmpty(transfer.item))
		{
			if (transfer.stockIn.TryTake(transfer.item, out StockCollectingInfo takeCollectingInfo))
			{
				float duration = transfer.stockIn.TransferDuration + transfer.stockOut.TransferDuration;

				if (transfer.stockOut.TryAdd(transfer.item, out StockCollectingInfo addCollectingInfo, duration))
				{
					if (duration > 0.0f)
					{
						_collectingEvents.OnTransfer.OnNext(new StockCollectingTransfer
						{
							originInfo = takeCollectingInfo,
							targetInfo = addCollectingInfo,

							prefab = _viewData.GetItemViewData(transfer.item.type).prefab,

							duration = duration
						});
					}

					return true;
				}
			}
		}
		else if (forced)
		{
			OnTransfer(transfer);
		}

		return false;
	}

	public void Dispose()
	{
		_compositeDisposable.Dispose();
	}
}