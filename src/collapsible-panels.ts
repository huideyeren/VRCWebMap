export type FormPanel = "add" | "edit";

/**
 * 同じ見出しなら閉じ、別の見出しなら排他的に切り替えます。
 */
export function toggleFormPanel(
    current: FormPanel | null,
    requested: FormPanel): FormPanel | null {
    return current === requested ? null : requested;
}
