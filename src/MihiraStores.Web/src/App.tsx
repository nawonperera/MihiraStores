import { useState, useEffect, useCallback } from 'react';
import type { Branch, Product, Stock, StockTransfer, CreateStockTransferRequest } from './types';
import { api, extractErrorMessage } from './services/api';
import { Header } from './components/Header';
import { BannerAlert } from './components/BannerAlert';
import { BranchStockView } from './components/BranchStockView';
import { CreateTransferForm } from './components/CreateTransferForm';
import { TransfersTable } from './components/TransfersTable';
import {
  Boxes,
  Truck,
  Building2,
  Package,
  AlertOctagon,
  RefreshCw,
} from 'lucide-react';

export default function App() {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [transfers, setTransfers] = useState<StockTransfer[]>([]);

  const [fromBranchId, setFromBranchId] = useState<number | null>(null);
  const [toBranchId, setToBranchId] = useState<number | null>(null);
  const [fromBranchStocks, setFromBranchStocks] = useState<Stock[]>([]);

  const [isLoadingInitial, setIsLoadingInitial] = useState(true);
  const [isLoadingStock, setIsLoadingStock] = useState(false);
  const [isSubmittingTransfer, setIsSubmittingTransfer] = useState(false);
  const [actionLoadingId, setActionLoadingId] = useState<number | null>(null);
  const [isRefreshingAll, setIsRefreshingAll] = useState(false);

  const [backendError, setBackendError] = useState<string | null>(null);
  const [banner, setBanner] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Manual stock loader triggered by user events or actions
  const refreshBranchStock = useCallback(async (branchId: number) => {
    setIsLoadingStock(true);
    try {
      const stockList = await api.getBranchStock(branchId);
      setFromBranchStocks(stockList);
    } catch (err) {
      const msg = extractErrorMessage(err);
      setBanner({ type: 'error', message: `Could not load stock for selected branch: ${msg}` });
    } finally {
      setIsLoadingStock(false);
    }
  }, []);

  // Manual full refresh triggered by user event
  const refreshAllData = useCallback(async () => {
    setIsRefreshingAll(true);
    setBackendError(null);
    try {
      const [branchesData, productsData, transfersData] = await Promise.all([
        api.getBranches(),
        api.getProducts(),
        api.getTransfers(),
      ]);

      setBranches(branchesData);
      setProducts(productsData);
      setTransfers(transfersData);

      if (fromBranchId) {
        const stockList = await api.getBranchStock(fromBranchId);
        setFromBranchStocks(stockList);
      }
    } catch (err) {
      setBackendError(extractErrorMessage(err));
    } finally {
      setIsRefreshingAll(false);
    }
  }, [fromBranchId]);

  // Initial load effect (asynchronous state resolution to prevent synchronous setState in effect)
  useEffect(() => {
    let isMounted = true;

    async function initialize() {
      try {
        const [branchesData, productsData, transfersData] = await Promise.all([
          api.getBranches(),
          api.getProducts(),
          api.getTransfers(),
        ]);

        if (!isMounted) return;

        setBranches(branchesData);
        setProducts(productsData);
        setTransfers(transfersData);

        const defaultFrom = branchesData.find((b) => b.isWarehouse) || branchesData[0];
        const defaultTo = branchesData.find((b) => !b.isWarehouse) || branchesData[1];

        if (defaultFrom) {
          setFromBranchId(defaultFrom.id);
        }
        if (defaultTo) {
          setToBranchId(defaultTo.id);
        }

        setBackendError(null);
      } catch (err) {
        if (!isMounted) return;
        setBackendError(extractErrorMessage(err));
      } finally {
        if (isMounted) {
          setIsLoadingInitial(false);
        }
      }
    }

    void initialize();

    return () => {
      isMounted = false;
    };
  }, []);

  // Fetch branch stock when fromBranchId changes
  useEffect(() => {
    if (fromBranchId === null) return;

    let isMounted = true;

    async function loadStock() {
      try {
        const stockList = await api.getBranchStock(fromBranchId!);
        if (!isMounted) return;
        setFromBranchStocks(stockList);
      } catch (err) {
        if (!isMounted) return;
        setBanner({
          type: 'error',
          message: `Could not load stock for selected branch: ${extractErrorMessage(err)}`,
        });
      }
    }

    void loadStock();

    return () => {
      isMounted = false;
    };
  }, [fromBranchId]);

  // Handle branch change by user
  const handleFromBranchChange = (newBranchId: number) => {
    setFromBranchId(newBranchId);
    void refreshBranchStock(newBranchId);
  };

  // 1. Create Transfer
  const handleCreateTransfer = async (request: CreateStockTransferRequest) => {
    setIsSubmittingTransfer(true);
    setBanner(null);
    try {
      const created = await api.createTransfer(request);
      setBanner({
        type: 'success',
        message: `Transfer #${created.id} successfully created in 'Requested' status.`,
      });

      // Reload transfers list and reload current branch stock
      const updatedTransfers = await api.getTransfers();
      setTransfers(updatedTransfers);
      if (fromBranchId) {
        await refreshBranchStock(fromBranchId);
      }
    } catch (err) {
      setBanner({ type: 'error', message: extractErrorMessage(err) });
    } finally {
      setIsSubmittingTransfer(false);
    }
  };

  // 2. Dispatch Transfer
  const handleDispatchTransfer = async (transferId: number) => {
    setActionLoadingId(transferId);
    setBanner(null);
    try {
      const result = await api.dispatchTransfer(transferId);
      setBanner({
        type: 'success',
        message: `Transfer #${result.id} successfully dispatched! Source branch inventory was deducted.`,
      });

      // Reload transfers and reload stock
      const updatedTransfers = await api.getTransfers();
      setTransfers(updatedTransfers);
      if (fromBranchId) {
        await refreshBranchStock(fromBranchId);
      }
    } catch (err) {
      setBanner({ type: 'error', message: extractErrorMessage(err) });
    } finally {
      setActionLoadingId(null);
    }
  };

  // 3. Receive Transfer
  const handleReceiveTransfer = async (transferId: number) => {
    setActionLoadingId(transferId);
    setBanner(null);
    try {
      const result = await api.receiveTransfer(transferId);
      setBanner({
        type: 'success',
        message: `Transfer #${result.id} successfully received! Destination branch inventory was updated.`,
      });

      // Reload transfers and reload stock
      const updatedTransfers = await api.getTransfers();
      setTransfers(updatedTransfers);
      if (fromBranchId) {
        await refreshBranchStock(fromBranchId);
      }
    } catch (err) {
      setBanner({ type: 'error', message: extractErrorMessage(err) });
    } finally {
      setActionLoadingId(null);
    }
  };

  // 4. Cancel Transfer
  const handleCancelTransfer = async (transferId: number) => {
    setActionLoadingId(transferId);
    setBanner(null);
    try {
      const result = await api.cancelTransfer(transferId);
      setBanner({
        type: 'success',
        message: `Transfer #${result.id} cancelled. No stock levels were affected.`,
      });

      // Reload transfers and reload stock
      const updatedTransfers = await api.getTransfers();
      setTransfers(updatedTransfers);
      if (fromBranchId) {
        await refreshBranchStock(fromBranchId);
      }
    } catch (err) {
      setBanner({ type: 'error', message: extractErrorMessage(err) });
    } finally {
      setActionLoadingId(null);
    }
  };

  const selectedFromBranch = branches.find((b) => b.id === fromBranchId) || null;

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col font-sans">
      <Header
        onRefreshAll={refreshAllData}
        isRefreshing={isRefreshingAll}
        totalTransfers={transfers.length}
      />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8">
        {/* Banner Alert for Success / Error */}
        {banner && (
          <BannerAlert
            type={banner.type}
            message={banner.message}
            onClose={() => setBanner(null)}
          />
        )}

        {/* Backend Unreachable Alert */}
        {backendError && (
          <div className="rounded-xl border border-rose-300 bg-rose-50 p-6 shadow-sm mb-6 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div className="flex items-start gap-3">
              <AlertOctagon className="w-6 h-6 text-rose-600 shrink-0 mt-0.5" />
              <div>
                <h3 className="text-base font-bold text-rose-900 m-0">Backend API Unreachable</h3>
                <p className="text-xs text-rose-700 mt-1 leading-relaxed">
                  {backendError}
                </p>
                <p className="text-xs text-slate-500 mt-1">
                  Ensure the ASP.NET Core Web API is launched using{' '}
                  <code className="bg-rose-100 text-rose-900 px-1 py-0.5 rounded font-mono">
                    dotnet run --project src/MihiraStores.Api
                  </code>.
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={refreshAllData}
              disabled={isRefreshingAll}
              className="inline-flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-700 active:bg-rose-800 text-white rounded-lg text-xs font-semibold shadow-xs disabled:opacity-50 transition shrink-0"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isRefreshingAll ? 'animate-spin' : ''}`} />
              <span>Retry Connection</span>
            </button>
          </div>
        )}

        {/* Initial Loading Screen */}
        {isLoadingInitial ? (
          <div className="py-24 flex flex-col items-center justify-center gap-3 text-slate-500">
            <div className="p-4 bg-indigo-50 rounded-2xl">
              <RefreshCw className="w-8 h-8 animate-spin text-indigo-600" />
            </div>
            <h2 className="text-base font-bold text-slate-800 m-0">Connecting to Mihira Stores...</h2>
            <p className="text-xs text-slate-400">Loading catalog, branch registries, and inventory levels</p>
          </div>
        ) : (
          <div className="space-y-8">
            {/* KPI Stats Bar */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
                <div className="p-2.5 bg-blue-50 text-blue-700 rounded-lg">
                  <Building2 className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-xs font-medium text-slate-500">Total Branches</div>
                  <div className="text-lg font-bold text-slate-900">{branches.length}</div>
                </div>
              </div>

              <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
                <div className="p-2.5 bg-indigo-50 text-indigo-700 rounded-lg">
                  <Package className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-xs font-medium text-slate-500">Catalog Products</div>
                  <div className="text-lg font-bold text-slate-900">{products.length}</div>
                </div>
              </div>

              <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
                <div className="p-2.5 bg-amber-50 text-amber-700 rounded-lg">
                  <Truck className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-xs font-medium text-slate-500">In Transit (Dispatched)</div>
                  <div className="text-lg font-bold text-slate-900">
                    {transfers.filter((t) => t.status === 'Dispatched' || t.status === 2).length}
                  </div>
                </div>
              </div>

              <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-3.5">
                <div className="p-2.5 bg-emerald-50 text-emerald-700 rounded-lg">
                  <Boxes className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-xs font-medium text-slate-500">Completed Transfers</div>
                  <div className="text-lg font-bold text-slate-900">
                    {transfers.filter((t) => t.status === 'Received' || t.status === 3).length}
                  </div>
                </div>
              </div>
            </div>

            {/* Top Row: Create Transfer Form & Real-time Branch Stock View */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
              {/* Transfer Form (7 cols) */}
              <div className="lg:col-span-7">
                <CreateTransferForm
                  branches={branches}
                  products={products}
                  fromBranchId={fromBranchId}
                  toBranchId={toBranchId}
                  onFromBranchChange={handleFromBranchChange}
                  onToBranchChange={(id) => setToBranchId(id)}
                  availableStocks={fromBranchStocks}
                  isSubmitting={isSubmittingTransfer}
                  onSubmitTransfer={handleCreateTransfer}
                />
              </div>

              {/* Real-time Branch Stock Inspector (5 cols) */}
              <div className="lg:col-span-5">
                <BranchStockView
                  branch={selectedFromBranch}
                  stocks={fromBranchStocks}
                  isLoading={isLoadingStock}
                  onRefreshStock={() => fromBranchId && refreshBranchStock(fromBranchId)}
                />
              </div>
            </div>

            {/* Bottom Row: Stock Transfers Table */}
            <div>
              <TransfersTable
                transfers={transfers}
                branches={branches}
                isLoading={isRefreshingAll}
                actionLoadingId={actionLoadingId}
                onDispatch={handleDispatchTransfer}
                onReceive={handleReceiveTransfer}
                onCancel={handleCancelTransfer}
              />
            </div>
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="mt-auto border-t border-slate-200 bg-white py-4 text-center text-xs text-slate-400">
        Mihira Stores &bull; Strict 3-Tier Inventory & Stock Transfer System
      </footer>
    </div>
  );
}
