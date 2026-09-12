import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

export default defineConfig([
  ...nextVitals,
  ...nextTs,

  // Global rules
  {
    rules: {
      /**
       * File size
       */
      "max-lines": [
        "warn",
        {
          max: 250,
          skipBlankLines: true,
          skipComments: true,
        },
      ],

      /**
       * Function size
       */
      "max-lines-per-function": [
        "warn",
        {
          max: 80,
          skipBlankLines: true,
          skipComments: true,
          IIFEs: true,
        },
      ],

      /**
       * Function complexity
       */
      complexity: [
        "warn",
        {
          max: 10,
        },
      ],

      /**
       * Maximum nesting
       */
      "max-depth": [
        "warn",
        {
          max: 3,
        },
      ],

      /**
       * Nested callbacks
       */
      "max-nested-callbacks": [
        "warn",
        {
          max: 3,
        },
      ],

      /**
       * Max parameters
       */
      "max-params": [
        "warn",
        {
          max: 4,
        },
      ],

      /**
       * Better code quality
       */
      "no-console": [
        "warn",
        {
          allow: ["warn", "error"],
        },
      ],

      "no-duplicate-imports": "warn",

      "no-nested-ternary": "warn",

      eqeqeq: ["warn", "always"],

      curly: ["warn", "all"],

      /**
       * Typescript
       */
      "@typescript-eslint/no-explicit-any": "warn",

      "@typescript-eslint/no-unused-vars": [
        "warn",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          ignoreRestSiblings: true,
        },
      ],

      /**
       * Prefer const
       */
      "prefer-const": "warn",

      /**
       * Prevent var
       */
      "no-var": "error",
    },
  },

  /**
   * Allow pages to be slightly larger.
   */
  {
    files: ["**/page.tsx"],
    rules: {
      "max-lines": [
        "warn",
        {
          max: 300,
          skipBlankLines: true,
          skipComments: true,
        },
      ],
    },
  },

  /**
   * Layouts
   */
  {
    files: ["**/layout.tsx"],
    rules: {
      "max-lines": [
        "warn",
        {
          max: 250,
          skipBlankLines: true,
          skipComments: true,
        },
      ],
    },
  },

  /**
   * Hooks
   */
  {
    files: ["**/hooks/**/*.ts", "**/hooks/**/*.tsx"],
    rules: {
      "max-lines": [
        "warn",
        {
          max: 180,
          skipBlankLines: true,
          skipComments: true,
        },
      ],
    },
  },

  /**
   * Components
   */
  {
    files: ["**/components/**/*.tsx"],
    rules: {
      "max-lines": [
        "warn",
        {
          max: 200,
          skipBlankLines: true,
          skipComments: true,
        },
      ],
    },
  },

  /**
   * Ignore generated/build files
   */
  globalIgnores([
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    "backend-cs/bin/**",
  ]),
]);