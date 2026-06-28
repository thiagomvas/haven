import js from "@eslint/js";
import tseslint from "typescript-eslint";
import reactHooks from "eslint-plugin-react-hooks";
import prettier from "eslint-plugin-prettier";
import prettierConfig from "eslint-config-prettier";
import simpleImportSort from "eslint-plugin-simple-import-sort";

export default tseslint.config(
  js.configs.recommended,
  ...tseslint.configs.recommended,
  prettierConfig,
  {
    plugins: {
      "react-hooks": reactHooks,
      prettier,
      "simple-import-sort": simpleImportSort,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "@typescript-eslint/no-unused-vars": "off",
      "@typescript-eslint/no-explicit-any": "off",
      "prettier/prettier": "warn",
      
      // Import sorting (auto-fixable)
      "simple-import-sort/imports": "warn",
      "simple-import-sort/exports": "warn",
      
      // Prevent direct imports from nested type files
      "no-restricted-imports": [
        "warn",
        {
          patterns: [
            {
              group: ["@/api/types/*"],
              message: "Import from @/api/types instead, then re-export what you need in the index file.",
            },
          ],
        },
      ],
    },
  }
);