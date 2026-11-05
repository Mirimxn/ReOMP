import "eslint-plugin-only-warn";

import globals from "globals";
import importAlias from "@dword-design/eslint-plugin-import-alias";
import importNewlines from "eslint-plugin-import-newlines";
import importQuotes from "eslint-plugin-import-quotes";
import js from "@eslint/js";
import perfectionist from "eslint-plugin-perfectionist";
import react from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import stylistic from "@stylistic/eslint-plugin";
import tailwindcss from "eslint-plugin-tailwindcss";
import typescriptEslint from "typescript-eslint";

export default typescriptEslint.config(
  {
    files: [
      `src/**/*.{ts,tsx}`,
    ],
    plugins: {
      [`@typescript-eslint`]: typescriptEslint.plugin,
      react,
      [`react-hooks`]: reactHooks,
      [`react-refresh`]: reactRefresh,
      tailwindcss,
      [`@stylistic`]: stylistic,
      [`@dword-design/import-alias`]: importAlias,
      perfectionist,
      [`import-newlines`]: importNewlines,
      [`import-quotes`]: importQuotes,
    },
    languageOptions: {
      globals: globals.browser,
      parser: typescriptEslint.parser,
      parserOptions: {
        project: [
          `./tsconfig.app.json`,
        ],
      },
    },
    settings: {
      react: {
        version: `18.2`,
      },
    },
    rules: {
      ...js.configs.recommended.rules,
      ...typescriptEslint.configs.strictTypeChecked.reduce((x, y) => ({ ...x, ...y.rules }), {}),
      ...typescriptEslint.configs.stylisticTypeChecked.reduce((x, y) => ({ ...x, ...y.rules }), {}),
      [`no-empty-pattern`]: `off`,
      [`@typescript-eslint/require-await`]: `off`,
      [`@typescript-eslint/only-throw-error`]: `off`,
      [`@typescript-eslint/consistent-indexed-object-style`]: `off`,
      [`@typescript-eslint/no-misused-promises`]: [
        `warn`,
        {
          checksVoidReturn: false,
        },
      ],
      [`@typescript-eslint/strict-boolean-expressions`]: [
        `warn`,
        {
          allowNullableObject: false,
          allowNumber: false,
          allowString: false,
        },
      ],
      ...react.configs.recommended.rules,
      ...react.configs[`jsx-runtime`].rules,
      ...reactHooks.configs.recommended.rules,
      [`react-refresh/only-export-components`]: [
        `warn`,
        {
          allowConstantExport: true,
        },
      ],
      ...tailwindcss.configs.recommended.rules,
      [`@dword-design/import-alias/prefer-alias`]: [
        `warn`,
        {
          alias: {
            [`@`]: `./src`,
          },
        },
      ],
      ...stylistic.configs.customize({
        indent: 2,
        quotes: `double`,
        semi: true,
        jsx: true,
        braceStyle: `1tbs`,
      }).rules,
      [`perfectionist/sort-imports`]: [
        `warn`,
        {
          internalPattern: [`@/**`],
          groups: [
            [
              `builtin-type`,
              `builtin`,
              `external-type`,
              `external`,
            ],
            [
              `internal-type`,
              `internal`,
            ],
            [
              `index-type`,
              `index`,
              `parent-type`,
              `parent`,
              `sibling-type`,
              `sibling`,
            ],
            `unknown`,
            [
              `side-effect-style`,
              `side-effect`,
            ],
          ],
        }
      ],
      [`perfectionist/sort-named-imports`]: [
        `warn`,
      ],
      [`import-newlines/enforce`]: [
        `warn`,
        {
          items: 1,
        },
      ],
      [`import-quotes/import-quotes`]: [
        `warn`,
        `double`,
      ],
    },
  },
);
