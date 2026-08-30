type LoadingHandler = () => void;

let beginLoadingHandler: LoadingHandler = () => {};
let endLoadingHandler: LoadingHandler = () => {};

export function configureApiLoading(options: {
  beginLoading: LoadingHandler;
  endLoading: LoadingHandler;
}): void {
  beginLoadingHandler = options.beginLoading;
  endLoadingHandler = options.endLoading;
}

export function beginApiLoading(): void {
  beginLoadingHandler();
}

export function endApiLoading(): void {
  endLoadingHandler();
}
